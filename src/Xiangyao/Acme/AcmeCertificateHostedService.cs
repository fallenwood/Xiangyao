namespace Xiangyao;

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Xiangyao.Acme;

internal sealed class AcmeCertificateHostedService(
  IAcmeDomainProvider domainProvider,
  IHttp01ChallengeStore challengeStore,
  IHttpClientFactory httpClientFactory,
  string email,
  string certificateDirectory,
  ILogger<AcmeCertificateHostedService> logger) : BackgroundService {

  private volatile X509Certificate2? certificate;
  private string[] lastDomainNames = [];

  public X509Certificate2? Certificate => this.certificate;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    this.TryLoadExistingCertificate();

    while (!stoppingToken.IsCancellationRequested) {
      try {
        var domains = domainProvider.DomainNames;

        if (domains.Length > 0 && (this.NeedsRenewal() || !domains.SequenceEqual(this.lastDomainNames))) {
          logger.LogInformation("Obtaining certificate for domains: {Domains}", string.Join(", ", domains));
          await this.ObtainCertificateAsync(domains, stoppingToken);
          this.lastDomainNames = [.. domains];
          logger.LogInformation("Certificate obtained successfully");
        }
      } catch (Exception ex) {
        logger.LogError(ex, "Failed to obtain certificate");
      }

      await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
    }
  }

  private async Task ObtainCertificateAsync(string[] domains, CancellationToken cancellationToken) {
    using var client = new AcmeClient(httpClientFactory.CreateClient());

    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.Http01,
      Http01Store = challengeStore,
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      email,
      certificateDirectory,
      options);

    var cert = await manager.ObtainCertificateAsync(domains, cancellationToken);
    manager.SaveCertificate(cert, "xiangyao");
    this.certificate = cert;
  }

  private void TryLoadExistingCertificate() {
    var pfxPath = Path.Combine(certificateDirectory, "xiangyao.pfx");

    if (!File.Exists(pfxPath)) {
      return;
    }

    try {
#pragma warning disable SYSLIB0057
      var cert = new X509Certificate2(pfxPath, "", X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057

      if (cert.NotAfter > DateTime.UtcNow) {
        this.certificate = cert;
        logger.LogInformation("Loaded existing certificate, expires {Expiry}", cert.NotAfter);
      } else {
        cert.Dispose();
        logger.LogInformation("Existing certificate has expired");
      }
    } catch (Exception ex) {
      logger.LogWarning(ex, "Failed to load existing certificate from {Path}", pfxPath);
    }
  }

  private bool NeedsRenewal() {
    if (this.certificate == null) {
      return true;
    }

    return this.certificate.NotAfter - DateTime.UtcNow < TimeSpan.FromDays(30);
  }
}
