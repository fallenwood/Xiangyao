namespace Xiangyao.Acme;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public interface ITlsAlpn01ChallengeStore {
  void AddChallenge(string domain, byte[] keyAuthorization);
  X509Certificate2? GetCertificate(string domain);
  void RemoveChallenge(string domain);
}

public class TlsAlpn01ChallengeStore : ITlsAlpn01ChallengeStore {
  private readonly ConcurrentDictionary<string, X509Certificate2> _challenges = new();

  public void AddChallenge(string domain, byte[] keyAuthorization) {
    var certificate = CreateSelfSignedCertificate(domain, keyAuthorization);
    _challenges[domain] = certificate;
  }

  public X509Certificate2? GetCertificate(string domain) {
    return _challenges.TryGetValue(domain, out var cert) ? cert : null;
  }

  public void RemoveChallenge(string domain) {
    if (_challenges.TryRemove(domain, out var cert)) {
      cert.Dispose();
    }
  }

  private static X509Certificate2 CreateSelfSignedCertificate(string domain, byte[] keyAuthorization) {
    using var rsa = RSA.Create(2048);

    var request = new CertificateRequest(
      $"CN={domain}",
      rsa,
      HashAlgorithmName.SHA256,
      RSASignaturePadding.Pkcs1);

    // Add acmeIdentifier extension (id-pe-acmeIdentifier: 1.3.6.1.5.5.7.1.31)
    var acmeIdentifierOid = new Oid("1.3.6.1.5.5.7.1.31");

    // keyAuthorization is already the SHA-256 digest
    // DER encode: OCTET STRING containing the SHA-256 digest
    var derValue = new byte[keyAuthorization.Length + 2];
    derValue[0] = 0x04; // OCTET STRING tag
    derValue[1] = (byte)keyAuthorization.Length;
    Array.Copy(keyAuthorization, 0, derValue, 2, keyAuthorization.Length);

    request.CertificateExtensions.Add(
      new X509Extension(acmeIdentifierOid, derValue, critical: true));

    // Add Subject Alternative Name
    var sanBuilder = new SubjectAlternativeNameBuilder();
    sanBuilder.AddDnsName(domain);
    request.CertificateExtensions.Add(sanBuilder.Build());

    var certificate = request.CreateSelfSigned(
      DateTimeOffset.UtcNow.AddDays(-1),
      DateTimeOffset.UtcNow.AddDays(1));

    return certificate;
  }
}
