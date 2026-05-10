namespace Xiangyao;

using Xiangyao.Acme;

internal enum Provider : int {
  None,
  File,
  Docker,
}

internal sealed record Options(
  string[] AcmeDomainNames,
  Provider Provider = Provider.Docker,
  bool UseAcmeCertificates = false,
  bool UseHttps = false,
  bool UseHttpsRedirect = false,
  string AcmeEmailAddress = "",
  AcmeCertificateAuthority CertificateAuthority = AcmeCertificateAuthority.LetsEncrypt,
  AcmeExternalAccountBindingOptions? ExternalAccountBinding = null,
  string Certificate = "",
  string CertificateKey = "",
  bool UseOtel = false,
  string OtelLogEndpoint = "",
  string OtelTraceEndpoint = "",
  string OtelMeterEndpoint = "",
  bool UsePortal = false,
  int PortalPort = 8080);
