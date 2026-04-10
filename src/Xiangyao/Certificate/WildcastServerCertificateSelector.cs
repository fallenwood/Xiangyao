namespace Xiangyao.Certificate;

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;

public interface IServerCertificateSelector {
  X509Certificate2? Select(ConnectionContext context, string? domainName);
}

public class WildcastServerCertificateSelector(string certificate, string certificateKey)
  : IServerCertificateSelector {
  private readonly Lazy<X509Certificate2> certificate = new (
    () => X509Certificate2.CreateFromPemFile(certificate, certificateKey),
    LazyThreadSafetyMode.ExecutionAndPublication);

  public X509Certificate2? Select(ConnectionContext context, string? domainName)
    => this.Certificate;

  public X509Certificate2 Certificate => this.certificate.Value;
}
