namespace Xiangyao.Acme;

using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that manages ACME certificate acquisition and renewal.
/// </summary>
public class AcmeCertificateRenewalService : BackgroundService {
  private readonly IAcmeOptionsProvider _optionsProvider;
  private readonly IHttp01ChallengeStore _challengeStore;
  private readonly ILogger<AcmeCertificateRenewalService> _logger;
  private readonly ConcurrentDictionary<string, X509Certificate2> _certificates = new();
  private readonly TimeSpan _checkInterval = TimeSpan.FromHours(12);
  private readonly TimeSpan _renewBeforeExpiry = TimeSpan.FromDays(30);

  private AcmeClient? _client;
  private AcmeCertificateManager? _manager;

  public AcmeCertificateRenewalService(
    IAcmeOptionsProvider optionsProvider,
    IHttp01ChallengeStore challengeStore,
    ILogger<AcmeCertificateRenewalService> logger) {
    _optionsProvider = optionsProvider;
    _challengeStore = challengeStore;
    _logger = logger;
  }

  /// <summary>
  /// Gets a certificate for the specified domain.
  /// </summary>
  public X509Certificate2? GetCertificate(string? domain) {
    if (string.IsNullOrEmpty(domain)) {
      return _certificates.Values.FirstOrDefault();
    }

    // Try exact match first
    if (_certificates.TryGetValue(domain, out var cert)) {
      return cert;
    }

    // Try wildcard match
    var parts = domain.Split('.');
    if (parts.Length >= 2) {
      var wildcardDomain = $"*.{string.Join('.', parts.Skip(1))}";
      if (_certificates.TryGetValue(wildcardDomain, out cert)) {
        return cert;
      }
    }

    // Return any available certificate as fallback
    return _certificates.Values.FirstOrDefault();
  }

  /// <summary>
  /// Gets all currently cached certificates.
  /// </summary>
  public IReadOnlyDictionary<string, X509Certificate2> Certificates => _certificates;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Initial delay

    while (!stoppingToken.IsCancellationRequested) {
      try {
        await CheckAndRenewCertificatesAsync(stoppingToken);
      } catch (Exception ex) {
        _logger.LogError(ex, "Error during certificate check/renewal");
      }

      await Task.Delay(_checkInterval, stoppingToken);
    }
  }

  private async Task CheckAndRenewCertificatesAsync(CancellationToken cancellationToken) {
    var domains = _optionsProvider.DomainNames;

    if (domains.Count == 0) {
      _logger.LogDebug("No domains configured for ACME certificates");
      return;
    }

    // Filter out domains with port numbers (Let's Encrypt doesn't support them)
    var validDomains = domains
      .Select(d => d.Contains(':') ? d.Split(':')[0] : d)
      .Where(d => !string.IsNullOrWhiteSpace(d))
      .Distinct()
      .ToArray();

    if (validDomains.Length == 0) {
      _logger.LogDebug("No valid domains after filtering");
      return;
    }

    _logger.LogInformation("Checking certificates for domains: {Domains}", string.Join(", ", validDomains));

    // Check if we need to renew
    var needsRenewal = validDomains.Any(NeedsCertificateRenewal);

    if (!needsRenewal && _certificates.Count > 0) {
      _logger.LogDebug("All certificates are still valid");
      return;
    }

    // Load existing certificates from disk
    await LoadExistingCertificatesAsync(validDomains, cancellationToken);

    // Check again after loading
    needsRenewal = validDomains.Any(NeedsCertificateRenewal);

    if (!needsRenewal && _certificates.Count > 0) {
      _logger.LogDebug("Loaded valid certificates from disk");
      return;
    }

    // Request new certificates
    await ObtainCertificatesAsync(validDomains, cancellationToken);
  }

  private bool NeedsCertificateRenewal(string domain) {
    if (!_certificates.TryGetValue(domain, out var cert)) {
      return true;
    }

    var expiresIn = cert.NotAfter - DateTime.UtcNow;
    return expiresIn < _renewBeforeExpiry;
  }

  private async Task LoadExistingCertificatesAsync(string[] domains, CancellationToken cancellationToken) {
    var certDir = _optionsProvider.CertificateDirectory;

    if (!Directory.Exists(certDir)) {
      return;
    }

    foreach (var domain in domains) {
      var pfxPath = Path.Combine(certDir, $"{SanitizeFilename(domain)}.pfx");

      if (!File.Exists(pfxPath)) {
        continue;
      }

      try {
        var pfxBytes = await File.ReadAllBytesAsync(pfxPath, cancellationToken);
#pragma warning disable SYSLIB0057
        var cert = new X509Certificate2(pfxBytes, "", X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057

        if (cert.NotAfter > DateTime.UtcNow) {
          _certificates[domain] = cert;
          _logger.LogInformation("Loaded certificate for {Domain}, expires {Expiry}", domain, cert.NotAfter);
        } else {
          _logger.LogInformation("Certificate for {Domain} has expired, will renew", domain);
          cert.Dispose();
        }
      } catch (Exception ex) {
        _logger.LogWarning(ex, "Failed to load certificate for {Domain}", domain);
      }
    }
  }

  private async Task ObtainCertificatesAsync(string[] domains, CancellationToken cancellationToken) {
    if (string.IsNullOrEmpty(_optionsProvider.EmailAddress)) {
      _logger.LogWarning("No email address configured for ACME");
      return;
    }

    try {
      _client ??= new AcmeClient(_optionsProvider.AcmeDirectoryUrl);
      _manager ??= new AcmeCertificateManager(
        _client,
        _challengeStore,
        _optionsProvider.EmailAddress,
        _optionsProvider.CertificateDirectory);

      _logger.LogInformation("Requesting certificate for {Domains}", string.Join(", ", domains));

      var cert = await _manager.ObtainCertificateAsync(domains, cancellationToken);

      // Save and cache certificate for each domain
      var primaryDomain = domains[0];
      _manager.SaveCertificate(cert, SanitizeFilename(primaryDomain));

      foreach (var domain in domains) {
        _certificates[domain] = cert;
      }

      _logger.LogInformation("Successfully obtained certificate for {Domains}, expires {Expiry}",
        string.Join(", ", domains), cert.NotAfter);
    } catch (Exception ex) {
      _logger.LogError(ex, "Failed to obtain certificate for {Domains}", string.Join(", ", domains));
    }
  }

  private static string SanitizeFilename(string domain) {
    return domain.Replace("*", "_wildcard_").Replace(":", "_");
  }

  public override void Dispose() {
    foreach (var cert in _certificates.Values) {
      cert.Dispose();
    }
    _client?.Dispose();
    base.Dispose();
  }
}
