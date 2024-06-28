namespace Xiangyao;

internal enum Provider : int {
  File,
  Docker,
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
  string OtelMeterEndpoint = "");
