namespace Xiangyao.Acme;

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Certificate selector that retrieves certificates from the ACME renewal service.
/// </summary>
public class AcmeCertificateSelector {
  private readonly AcmeCertificateRenewalService _renewalService;

  public AcmeCertificateSelector(AcmeCertificateRenewalService renewalService) {
    _renewalService = renewalService;
  }

  public X509Certificate2? SelectCertificate(ConnectionContext? context, string? domainName) {
    return _renewalService.GetCertificate(domainName);
  }
}

/// <summary>
/// Extension methods for Kestrel HTTPS configuration with ACME.
/// </summary>
public static class AcmeKestrelExtensions {
  /// <summary>
  /// Configures Kestrel to use ACME certificates.
  /// </summary>
  public static HttpsConnectionAdapterOptions UseAcmeCertificates(
    this HttpsConnectionAdapterOptions options,
    IServiceProvider serviceProvider) {
    var renewalService = serviceProvider.GetRequiredService<AcmeCertificateRenewalService>();
    var selector = new AcmeCertificateSelector(renewalService);

    options.ServerCertificateSelector = selector.SelectCertificate;

    return options;
  }
}
