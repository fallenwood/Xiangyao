using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using BcX509CertificateParser = Org.BouncyCastle.X509.X509CertificateParser;

namespace Xiangyao.Acme;

public class AcmeCertificateManager {
  private readonly AcmeClient _client;
  private readonly IHttp01ChallengeStore _challengeStore;
  private readonly string _email;
  private readonly string _certificateDirectory;

  public AcmeCertificateManager(
    AcmeClient client,
    IHttp01ChallengeStore challengeStore,
    string email,
    string certificateDirectory) {
    _client = client;
    _challengeStore = challengeStore;
    _email = email;
    _certificateDirectory = certificateDirectory;
    
    Directory.CreateDirectory(_certificateDirectory);
  }

  public async Task<X509Certificate2> ObtainCertificateAsync(string[] domainNames, CancellationToken cancellationToken = default) {
    await _client.InitializeAsync(cancellationToken);
    
    await _client.CreateAccountAsync([_email], termsOfServiceAgreed: true, cancellationToken);
    
    var order = await _client.CreateOrderAsync(domainNames, cancellationToken);
    
    foreach (var authzUrl in order.Authorizations) {
      var authz = await _client.GetAuthorizationAsync(authzUrl, cancellationToken);
      var challenge = authz.Challenges.FirstOrDefault(c => c.Type == "http-01");
      if (challenge == null) {
        throw new AcmeException($"No HTTP-01 challenge found for {authz.Identifier.Value}");
      }

      var keyAuth = _client.GetKeyAuthorization(challenge.Token);
      _challengeStore.AddChallenge(challenge.Token, keyAuth);

      await _client.CompleteChallengeAsync(challenge.Url, cancellationToken);
      
      await WaitForAuthorizationAsync(authzUrl, cancellationToken);
      
      _challengeStore.RemoveChallenge(challenge.Token);
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
