namespace Xiangyao.Acme;

using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
  JsonSerializerDefaults.Web,
  PropertyNameCaseInsensitive = true,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AcmeDirectory))]
[JsonSerializable(typeof(AcmeAccount))]
[JsonSerializable(typeof(AcmeOrder))]
[JsonSerializable(typeof(AcmeAuthorization))]
[JsonSerializable(typeof(AcmeChallenge))]
[JsonSerializable(typeof(AcmeError))]
[JsonSerializable(typeof(AcmeExternalAccountBindingOptions))]
[JsonSerializable(typeof(ZeroSslExternalAccountBindingResponse))]
[JsonSerializable(typeof(AcmeAccountPayload))]
[JsonSerializable(typeof(AcmeCreateOrderPayload))]
[JsonSerializable(typeof(AcmeFinalizeOrderPayload))]
[JsonSerializable(typeof(AcmeEmptyPayload))]
[JsonSerializable(typeof(AcmeJwk))]
[JsonSerializable(typeof(AcmeJwsHeader))]
[JsonSerializable(typeof(AcmeExternalAccountBindingJwsHeader))]
[JsonSerializable(typeof(AcmeJwsEnvelope))]
[JsonSerializable(typeof(CloudflareCreateDnsRecordPayload))]
[JsonSerializable(typeof(CloudflareListResponse))]
internal sealed partial class AcmeJsonContext : JsonSerializerContext {
}

internal sealed record AcmeAccountPayload(
  string[] Contact,
  bool TermsOfServiceAgreed,
  AcmeJwsEnvelope? ExternalAccountBinding = null);

internal sealed record AcmeCreateOrderPayload(AcmeIdentifier[] Identifiers);

internal sealed record AcmeFinalizeOrderPayload(string Csr);

internal sealed record AcmeEmptyPayload;

internal sealed record AcmeJwk(string E, string Kty, string N);

internal sealed record AcmeJwsHeader(
  string Alg,
  string Nonce,
  string Url,
  string? Kid = null,
  AcmeJwk? Jwk = null);

internal sealed record AcmeExternalAccountBindingJwsHeader(
  string Alg,
  string Kid,
  string Url);

internal sealed record AcmeJwsEnvelope(
  [property: JsonPropertyName("protected")] string Protected,
  string Payload,
  string Signature);

internal sealed record CloudflareCreateDnsRecordPayload(string Type, string Name, string Content, int Ttl);

internal sealed record CloudflareListResponse(CloudflareDnsRecord[] Result);

internal sealed record CloudflareDnsRecord(string Id, string Name, string Content);
