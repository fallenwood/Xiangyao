namespace Xiangyao;

internal enum Provider : int {
  None,
  File,
  Docker,
}

internal enum AcmeChallengeMode : int {
  Http01,
  Dns01,
  TlsAlpn01,
}

internal sealed record Options(
  string[] LetsEncryptDomainNames,
  Provider Provider = Provider.Docker,
  bool UseLetsEncrypt = false,
  bool UseHttps = false,
  bool UseHttpsRedirect = false,
  string LetsEncryptEmailAddress = "",
  string Certificate = "",
  string CertificateKey = "",
  bool UseOtel = false,
  string OtelLogEndpoint = "",
  string OtelTraceEndpoint = "",
  string OtelMeterEndpoint = "",
  bool UsePortal = false,
  int PortalPort = 8080,
  // New ACME options
  bool UseAcme = false,
  AcmeChallengeMode AcmeChallengeMode = AcmeChallengeMode.Http01,
  string AcmeEmail = "",
  string[] AcmeDomains = default!,
  string AcmeDirectoryUrl = "https://acme-v02.api.letsencrypt.org/directory",
  string AcmeCertificateDirectory = "./certificates");
