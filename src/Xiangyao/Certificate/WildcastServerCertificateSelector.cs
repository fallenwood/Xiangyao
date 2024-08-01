namespace Xiangyao.Certificate;

using McMaster.AspNetCore.Kestrel.Certificates;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;

public class WildcastServerCertificateSelector(string certificate, string certificateKey) : IServerCertificateSelector {
  private readonly Lazy<X509Certificate2> certificate = new (() => X509Certificate2.CreateFromPemFile(certificate, certificateKey), LazyThreadSafetyMode.ExecutionAndPublication);

  public X509Certificate2? Select(ConnectionContext context, string? domainName) {
    return this.certificate.Value;
  }
}
