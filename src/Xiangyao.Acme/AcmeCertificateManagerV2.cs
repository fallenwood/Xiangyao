namespace Xiangyao.Acme;

using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using BcX509CertificateParser = Org.BouncyCastle.X509.X509CertificateParser;


public interface IDnsProvider {
  Task CreateTxtRecordAsync(string domain, string name, string value, CancellationToken cancellationToken = default);
  Task DeleteTxtRecordAsync(string domain, string name, CancellationToken cancellationToken = default);
}

public class AcmeCertificateManagerOptions {
  public ChallengeType PreferredChallengeType { get; set; } = ChallengeType.Http01;
  public IHttp01ChallengeStore? Http01Store { get; set; }
  public IDns01ChallengeStore? Dns01Store { get; set; }
  public ITlsAlpn01ChallengeStore? TlsAlpn01Store { get; set; }
  public IDnsProvider? DnsProvider { get; set; }
}

public class AcmeCertificateManagerV2 {
  private readonly AcmeClient _client;
  private readonly string _email;
  private readonly string _certificateDirectory;
  private readonly AcmeCertificateManagerOptions _options;

  public AcmeCertificateManagerV2(
    AcmeClient client,
    string email,
    string certificateDirectory,
    AcmeCertificateManagerOptions options) {
    _client = client;
    _email = email;
    _certificateDirectory = certificateDirectory;
    _options = options;

    Directory.CreateDirectory(_certificateDirectory);
  }

  public async Task<X509Certificate2> ObtainCertificateAsync(
    string[] domainNames,
    CancellationToken cancellationToken = default) {
    await _client.InitializeAsync(cancellationToken);

    await _client.CreateAccountAsync([_email], termsOfServiceAgreed: true, cancellationToken);

    var order = await _client.CreateOrderAsync(domainNames, cancellationToken);

    foreach (var authzUrl in order.Authorizations) {
      var authz = await _client.GetAuthorizationAsync(authzUrl, cancellationToken);
      await ProcessAuthorizationAsync(authz, cancellationToken);
    }

    var certKeyPair = GenerateRsaKeyPair();
    var finalizedOrder = await _client.FinalizeOrderAsync(order.Finalize, certKeyPair, domainNames, cancellationToken);

    await WaitForCertificateAsync(finalizedOrder.OrderUrl!, cancellationToken);

    if (finalizedOrder.Certificate == null) {
      var updatedOrder = await GetOrderAsync(finalizedOrder.OrderUrl!, cancellationToken);
      finalizedOrder = finalizedOrder with { Certificate = updatedOrder.Certificate };
    }

    if (finalizedOrder.Certificate == null) {
      throw new AcmeException("Certificate URL not available");
    }

    var certificatePem = await _client.DownloadCertificateAsync(finalizedOrder.Certificate, cancellationToken);

    return ConvertToPfx(certificatePem, certKeyPair, domainNames[0]);
  }

  private async Task ProcessAuthorizationAsync(AcmeAuthorization authz, CancellationToken cancellationToken) {
    AcmeChallenge? challenge = null;
    string? challengeType = null;

    // Try to find preferred challenge type first
    switch (_options.PreferredChallengeType) {
      case ChallengeType.Http01:
        challenge = authz.Challenges.FirstOrDefault(c => c.Type == "http-01");
        challengeType = "http-01";
        break;
      case ChallengeType.Dns01:
        challenge = authz.Challenges.FirstOrDefault(c => c.Type == "dns-01");
        challengeType = "dns-01";
        break;
      case ChallengeType.TlsAlpn01:
        challenge = authz.Challenges.FirstOrDefault(c => c.Type == "tls-alpn-01");
        challengeType = "tls-alpn-01";
        break;
    }

    // Fallback to any available challenge
    if (challenge == null) {
      challenge = authz.Challenges.FirstOrDefault(c => c.Type == "http-01");
      challengeType = "http-01";
    }

    if (challenge == null) {
      challenge = authz.Challenges.FirstOrDefault(c => c.Type == "dns-01");
      challengeType = "dns-01";
    }

    if (challenge == null) {
      challenge = authz.Challenges.FirstOrDefault(c => c.Type == "tls-alpn-01");
      challengeType = "tls-alpn-01";
    }

    if (challenge == null) {
      throw new AcmeException($"No supported challenge found for {authz.Identifier.Value}");
    }

    await SetupChallengeAsync(authz.Identifier.Value, challenge, challengeType!, cancellationToken);

    await _client.CompleteChallengeAsync(challenge.Url, cancellationToken);

    var authzUrl = authz.Challenges.First().Url;
    var baseUrl = authzUrl.Substring(0, authzUrl.LastIndexOf('/'));
    await WaitForAuthorizationAsync(baseUrl, cancellationToken);

    await CleanupChallengeAsync(authz.Identifier.Value, challengeType!, cancellationToken);
  }

  private async Task SetupChallengeAsync(
    string domain,
    AcmeChallenge challenge,
    string challengeType,
    CancellationToken cancellationToken) {
    switch (challengeType) {
      case "http-01":
        if (_options.Http01Store == null) {
          throw new AcmeException("HTTP-01 store not configured");
        }
        var keyAuth = _client.GetKeyAuthorization(challenge.Token);
        _options.Http01Store.AddChallenge(challenge.Token, keyAuth);
        break;

      case "dns-01":
        if (_options.Dns01Store == null || _options.DnsProvider == null) {
          throw new AcmeException("DNS-01 store or provider not configured");
        }
        var txtRecord = _client.GetDns01TxtRecord(challenge.Token);
        _options.Dns01Store.AddChallenge(domain, txtRecord);
        await _options.DnsProvider.CreateTxtRecordAsync(domain, "_acme-challenge", txtRecord, cancellationToken);
        // Wait for DNS propagation
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        break;

      case "tls-alpn-01":
        if (_options.TlsAlpn01Store == null) {
          throw new AcmeException("TLS-ALPN-01 store not configured");
        }
        var keyAuthHash = _client.GetTlsAlpn01KeyAuthorizationHash(challenge.Token);
        _options.TlsAlpn01Store.AddChallenge(domain, keyAuthHash);
        break;

      default:
        throw new AcmeException($"Unsupported challenge type: {challengeType}");
    }
  }

  private async Task CleanupChallengeAsync(
    string domain,
    string challengeType,
    CancellationToken cancellationToken) {
    switch (challengeType) {
      case "http-01":
        // HTTP-01 cleanup happens via token, but we don't have it here
        // In practice, challenges are short-lived and can be cleaned up later
        break;

      case "dns-01":
        if (_options.DnsProvider != null) {
          await _options.DnsProvider.DeleteTxtRecordAsync(domain, "_acme-challenge", cancellationToken);
        }
        _options.Dns01Store?.RemoveChallenge(domain);
        break;

      case "tls-alpn-01":
        _options.TlsAlpn01Store?.RemoveChallenge(domain);
        break;
    }
  }

  private async Task WaitForAuthorizationAsync(string authzUrl, CancellationToken cancellationToken) {
    for (int i = 0; i < 30; i++) {
      await Task.Delay(2000, cancellationToken);
      var authz = await _client.GetAuthorizationAsync(authzUrl, cancellationToken);

      if (authz.Status == "valid") {
        return;
      }

      if (authz.Status == "invalid") {
        var failedChallenge = authz.Challenges.FirstOrDefault(c => c.Error != null);
        var errorMsg = failedChallenge?.Error?.Detail ?? "Unknown error";
        throw new AcmeException($"Authorization failed: {errorMsg}");
      }
    }

    throw new AcmeException("Authorization timed out");
  }

  private async Task WaitForCertificateAsync(string orderUrl, CancellationToken cancellationToken) {
    for (int i = 0; i < 30; i++) {
      await Task.Delay(2000, cancellationToken);
      var order = await GetOrderAsync(orderUrl, cancellationToken);

      if (order.Status == "valid" && order.Certificate != null) {
        return;
      }

      if (order.Status == "invalid") {
        throw new AcmeException("Order became invalid");
      }
    }

    throw new AcmeException("Certificate issuance timed out");
  }

  private async Task<AcmeOrder> GetOrderAsync(string orderUrl, CancellationToken cancellationToken) {
    return await _client.GetOrderAsync(orderUrl, cancellationToken);
  }

  private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int keySize = 2048) {
    var generator = Org.BouncyCastle.Security.GeneratorUtilities.GetKeyPairGenerator("RSA");
    generator.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), keySize));
    return generator.GenerateKeyPair();
  }

  private X509Certificate2 ConvertToPfx(string certificatePem, AsymmetricCipherKeyPair keyPair, string friendlyName) {
    var certParser = new BcX509CertificateParser();
    var certificates = certParser.ReadCertificates(System.Text.Encoding.UTF8.GetBytes(certificatePem));

    var store = new Pkcs12StoreBuilder().Build();
    var certEntry = new X509CertificateEntry((BcX509Certificate)certificates[0]!);
    store.SetCertificateEntry(friendlyName, certEntry);

    var keyEntry = new AsymmetricKeyEntry(keyPair.Private);
    store.SetKeyEntry(friendlyName, keyEntry, [certEntry]);

    using var pfxStream = new MemoryStream();
    store.Save(pfxStream, Array.Empty<char>(), new Org.BouncyCastle.Security.SecureRandom());
    pfxStream.Position = 0;

#pragma warning disable SYSLIB0057
    return new X509Certificate2(pfxStream.ToArray(), "", X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
  }

  public void SaveCertificate(X509Certificate2 certificate, string filename) {
    var pfxPath = Path.Combine(_certificateDirectory, $"{filename}.pfx");
    var pfxBytes = certificate.Export(X509ContentType.Pfx, "");
    File.WriteAllBytes(pfxPath, pfxBytes);

    var pemPath = Path.Combine(_certificateDirectory, $"{filename}.pem");
    var pemBytes = System.Text.Encoding.UTF8.GetBytes(certificate.ExportCertificatePem());
    File.WriteAllBytes(pemPath, pemBytes);
  }
}
