namespace Xiangyao; 

using System.CommandLine;

internal sealed class OptionBindings {
  private const string DefaultOtelEndpoint = "http://localhost:4317";

  public readonly Option<Provider> providerOption = new(
    aliases: ["-p", "--provider"],
    getDefaultValue: () => Provider.Docker,
    description: "Config Provider (e.g. File, Docker, etc.)");

  public readonly Option<bool> useHttps = new Option<bool>(
    aliases: ["--https", "--use-https"],
    getDefaultValue: () => false,
    description: "Use HTTPS");

  public readonly Option<bool> useHttpsRedirect = new Option<bool>(
    aliases: ["--https-redirect", "--use-https-redirect"],
    getDefaultValue: () => false,
    description: "Use HTTPS Redirect");

  public readonly Option<bool> useLetsEncrypt = new Option<bool>(
    aliases: ["--lets-encrypt", "--use-lets-encrypt"],
    getDefaultValue: () => false,
    description: "Use Let's Encrypt");

  public readonly Option<IEnumerable<string>> letsEncryptDomainNames = new Option<IEnumerable<string>>(
    aliases: ["--lets-encrypt-domain-names"],
    getDefaultValue: () => [],
    description: "Let's Encrypt Domain Names");

  public readonly Option<string> letsEncryptEmailAddress = new Option<string>(
    aliases: ["--lets-encrypt-email", "--lets-encrypt-email-address"],
    getDefaultValue: () => "",
    description: "Let's Encrypt Email Address");

  public readonly Option<bool> useOtel = new (
    aliases: ["--use-otel"],
    getDefaultValue: () => false,
    description: "Use Opentelemetry");
    
  public readonly Option<string> otelLogEndpoint = new (
    aliases: ["--otel-log", "--otel-log-endpoint"],
    getDefaultValue: () => DefaultOtelEndpoint,
    description: "Opentelemetry Logs Endpoint");
    
  public readonly Option<string> otelTraceEndpoint = new (
    aliases: ["--otel-trace", "--otel-trace-endpoint"],
    getDefaultValue: () => DefaultOtelEndpoint,
    description: "Opentelemetry Trace Endpoint");
    
  public readonly Option<string> otelMeterEndpoint = new (
    aliases: ["--otel-meter", "--otel-meter-endpoint"],
    getDefaultValue: () => DefaultOtelEndpoint,
    description: "Opentelemetry Meter Endpoint");

  public readonly Option<string> certificate = new(
    aliases: ["--certificate", "--certificate-path"],
    getDefaultValue: () => string.Empty,
    description: "The fullchain.pem");

  public readonly Option<string> certificateKey = new(
    aliases: ["--certificate-key", "--certificate-key-path"],
    getDefaultValue: () => string.Empty,
    description: "The privkey.pem");
}
