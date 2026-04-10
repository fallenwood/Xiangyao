namespace Xiangyao.Acme;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;


public class AcmeClient : IDisposable {
  private readonly HttpClient _httpClient;
  private readonly AsymmetricCipherKeyPair _accountKey;
  private readonly string _directoryUrl;
  private readonly AcmeEmptyPayload _emptyPayload = new();
  private AcmeDirectory? _directory;
  private string? _nonce;
  private string? _kid;

  public AcmeClient(string directoryUrl = "https://acme-v02.api.letsencrypt.org/directory")
    : this(new HttpClient(), GenerateRsaKeyPair(), directoryUrl) {
  }

  public AcmeClient(HttpClient httpClient, string directoryUrl = "https://acme-v02.api.letsencrypt.org/directory")
    : this(httpClient, GenerateRsaKeyPair(), directoryUrl) {
  }

  public AcmeClient(AsymmetricCipherKeyPair accountKey, string directoryUrl = "https://acme-v02.api.letsencrypt.org/directory")
    : this(new HttpClient(), accountKey, directoryUrl) {
  }

  public AcmeClient(HttpClient httpClient, AsymmetricCipherKeyPair accountKey, string directoryUrl = "https://acme-v02.api.letsencrypt.org/directory") {
    _httpClient = httpClient;
    _directoryUrl = directoryUrl;
    _accountKey = accountKey;
  }

  public async Task InitializeAsync(CancellationToken cancellationToken = default) {
    _directory = await _httpClient.GetFromJsonAsync(_directoryUrl, AcmeJsonContext.Default.AcmeDirectory, cancellationToken);
    if (_directory == null) {
      throw new AcmeException("Failed to fetch ACME directory");
    }
    await GetNonceAsync(cancellationToken);
  }

  public async Task<AcmeAccount> CreateAccountAsync(string[] emailAddresses, bool termsOfServiceAgreed, CancellationToken cancellationToken = default) {
    if (_directory == null) throw new InvalidOperationException("Client not initialized");

    var payload = new AcmeAccountPayload(
      Contact: emailAddresses.Select(e => $"mailto:{e}").ToArray(),
      TermsOfServiceAgreed: termsOfServiceAgreed);

    var response = await SendSignedRequestAsync(
      _directory.NewAccount,
      JsonSerializer.Serialize(payload, AcmeJsonContext.Default.AcmeAccountPayload),
      useKid: false,
      cancellationToken: cancellationToken);
    _kid = response.Headers.Location?.ToString();

    var account = await response.Content.ReadFromJsonAsync(AcmeJsonContext.Default.AcmeAccount, cancellationToken);
    return account ?? throw new AcmeException("Failed to create account");
  }

  public async Task<AcmeOrder> CreateOrderAsync(string[] domainNames, CancellationToken cancellationToken = default) {
    if (_directory == null) throw new InvalidOperationException("Client not initialized");
    if (_kid == null) throw new InvalidOperationException("Account not created");

    var payload = new AcmeCreateOrderPayload(
      Identifiers: domainNames.Select(d => new AcmeIdentifier("dns", d)).ToArray());

    var response = await SendSignedRequestAsync(
      _directory.NewOrder,
      JsonSerializer.Serialize(payload, AcmeJsonContext.Default.AcmeCreateOrderPayload),
      useKid: true,
      cancellationToken: cancellationToken);
    var order = await response.Content.ReadFromJsonAsync(AcmeJsonContext.Default.AcmeOrder, cancellationToken);
    if (order != null) {
      order.OrderUrl = response.Headers.Location?.ToString();
    }
    return order ?? throw new AcmeException("Failed to create order");
  }

  public async Task<AcmeAuthorization> GetAuthorizationAsync(string authorizationUrl, CancellationToken cancellationToken = default) {
    var response = await SendSignedRequestAsync(authorizationUrl, string.Empty, useKid: true, cancellationToken: cancellationToken);
    var authorization = await response.Content.ReadFromJsonAsync(AcmeJsonContext.Default.AcmeAuthorization, cancellationToken);
    return authorization ?? throw new AcmeException("Failed to get authorization");
  }

  public async Task<AcmeChallenge> CompleteChallengeAsync(string challengeUrl, CancellationToken cancellationToken = default) {
    var response = await SendSignedRequestAsync(
      challengeUrl,
      JsonSerializer.Serialize(_emptyPayload, AcmeJsonContext.Default.AcmeEmptyPayload),
      useKid: true,
      cancellationToken: cancellationToken);
    var challenge = await response.Content.ReadFromJsonAsync(AcmeJsonContext.Default.AcmeChallenge, cancellationToken);
    return challenge ?? throw new AcmeException("Failed to complete challenge");
  }

  public async Task<AcmeOrder> FinalizeOrderAsync(string finalizeUrl, AsymmetricCipherKeyPair certificateKey, string[] domainNames, CancellationToken cancellationToken = default) {
    var csr = GenerateCsr(certificateKey, domainNames);
    var csrBase64 = Base64UrlEncode(csr);

    var payload = new AcmeFinalizeOrderPayload(Csr: csrBase64);
    var response = await SendSignedRequestAsync(
      finalizeUrl,
      JsonSerializer.Serialize(payload, AcmeJsonContext.Default.AcmeFinalizeOrderPayload),
      useKid: true,
      cancellationToken: cancellationToken);
    var order = await response.Content.ReadFromJsonAsync(AcmeJsonContext.Default.AcmeOrder, cancellationToken);
    if (order != null) {
      order.OrderUrl = response.Headers.Location?.ToString();
    }
    return order ?? throw new AcmeException("Failed to finalize order");
  }

  public async Task<AcmeOrder> GetOrderAsync(string orderUrl, CancellationToken cancellationToken = default) {
    var response = await SendSignedRequestAsync(orderUrl, string.Empty, useKid: true, cancellationToken: cancellationToken);
    var order = await response.Content.ReadFromJsonAsync(AcmeJsonContext.Default.AcmeOrder, cancellationToken);
    return order ?? throw new AcmeException("Failed to get order");
  }

  public async Task<string> DownloadCertificateAsync(string certificateUrl, CancellationToken cancellationToken = default) {
    var response = await SendSignedRequestAsync(certificateUrl, "", useKid: true, cancellationToken: cancellationToken);
    return await response.Content.ReadAsStringAsync(cancellationToken);
  }

  public string GetKeyAuthorization(string token) {
    var jwkThumbprint = GetJwkThumbprint();
    return $"{token}.{jwkThumbprint}";
  }

  public string GetDns01TxtRecord(string token) {
    var keyAuth = GetKeyAuthorization(token);
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyAuth));
    return Base64UrlEncode(hash);
  }

  public byte[] GetTlsAlpn01KeyAuthorizationHash(string token) {
    var keyAuth = GetKeyAuthorization(token);
    return SHA256.HashData(Encoding.UTF8.GetBytes(keyAuth));
  }

  private async Task<HttpResponseMessage> SendSignedRequestAsync(string url, string payload, bool useKid, CancellationToken cancellationToken) {
    if (_nonce == null) {
      await GetNonceAsync(cancellationToken);
    }

    var jws = CreateJws(url, payload, useKid);

    var content = new StringContent(JsonSerializer.Serialize(jws, AcmeJsonContext.Default.AcmeJwsEnvelope), Encoding.UTF8);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/jose+json");
    var response = await _httpClient.PostAsync(url, content, cancellationToken);

    if (response.Headers.TryGetValues("Replay-Nonce", out var nonces)) {
      _nonce = nonces.FirstOrDefault();
    }

    if (!response.IsSuccessStatusCode) {
      var error = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new AcmeException($"ACME request failed: {response.StatusCode} - {error}");
    }

    return response;
  }

  private async Task GetNonceAsync(CancellationToken cancellationToken) {
    if (_directory?.NewNonce == null) {
      throw new InvalidOperationException("Directory not initialized");
    }

    var request = new HttpRequestMessage(HttpMethod.Head, _directory.NewNonce);
    var response = await _httpClient.SendAsync(request, cancellationToken);
    if (response.Headers.TryGetValues("Replay-Nonce", out var nonces)) {
      _nonce = nonces.FirstOrDefault();
    }
  }

  private AcmeJwsEnvelope CreateJws(string url, string payload, bool useKid) {
    var header = useKid
      ? new AcmeJwsHeader(
        Alg: "RS256",
        Nonce: _nonce!,
        Url: url,
        Kid: _kid!)
      : new AcmeJwsHeader(
        Alg: "RS256",
        Nonce: _nonce!,
        Url: url,
        Jwk: GetJwk());

    var headerBase64 = Base64UrlEncode(JsonSerializer.Serialize(header, AcmeJsonContext.Default.AcmeJwsHeader));
    var payloadBase64 = payload == "" ? "" : Base64UrlEncode(payload);

    var signatureInput = $"{headerBase64}.{payloadBase64}";
    var signature = SignData(signatureInput);

    return new AcmeJwsEnvelope(
      Protected: headerBase64,
      Payload: payloadBase64,
      Signature: Base64UrlEncode(signature));
  }

  private AcmeJwk GetJwk() {
    var rsaKey = (RsaKeyParameters)_accountKey.Public;
    return new AcmeJwk(
      E: Base64UrlEncode(rsaKey.Exponent.ToByteArrayUnsigned()),
      Kty: "RSA",
      N: Base64UrlEncode(rsaKey.Modulus.ToByteArrayUnsigned()));
  }

  private string GetJwkThumbprint() {
    var jwk = GetJwk();
    var jwkJson = JsonSerializer.Serialize(jwk, AcmeJsonContext.Default.AcmeJwk);
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(jwkJson));
    return Base64UrlEncode(hash);
  }

  private byte[] SignData(string data) {
    var signer = SignerUtilities.GetSigner("SHA256withRSA");
    signer.Init(true, _accountKey.Private);
    var dataBytes = Encoding.UTF8.GetBytes(data);
    signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
    return signer.GenerateSignature();
  }

  private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int keySize = 2048) {
    var generator = GeneratorUtilities.GetKeyPairGenerator("RSA");
    generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
    return generator.GenerateKeyPair();
  }

  private static byte[] GenerateCsr(AsymmetricCipherKeyPair keyPair, string[] domainNames) {
    var subject = new Org.BouncyCastle.Asn1.X509.X509Name($"CN={domainNames[0]}");
    var pkcs10 = new Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest(
      "SHA256withRSA",
      subject,
      keyPair.Public,
      null,
      keyPair.Private);

    return pkcs10.GetEncoded();
  }

  private static string Base64UrlEncode(byte[] data) {
    return Convert.ToBase64String(data)
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '_');
  }

  private static string Base64UrlEncode(string data) {
    return Base64UrlEncode(Encoding.UTF8.GetBytes(data));
  }

  public void Dispose() {
    _httpClient?.Dispose();
  }
}

public record AcmeDirectory(
  [property: JsonPropertyName("newNonce")] string NewNonce,
  [property: JsonPropertyName("newAccount")] string NewAccount,
  [property: JsonPropertyName("newOrder")] string NewOrder,
  [property: JsonPropertyName("revokeCert")] string? RevokeCert = null,
  [property: JsonPropertyName("keyChange")] string? KeyChange = null
);

public record AcmeAccount(
  [property: JsonPropertyName("status")] string Status,
  [property: JsonPropertyName("contact")] string[] Contact,
  [property: JsonPropertyName("orders")] string? Orders = null
);

public record AcmeOrder(
  [property: JsonPropertyName("status")] string Status,
  [property: JsonPropertyName("expires")] DateTime? Expires,
  [property: JsonPropertyName("identifiers")] AcmeIdentifier[] Identifiers,
  [property: JsonPropertyName("authorizations")] string[] Authorizations,
  [property: JsonPropertyName("finalize")] string Finalize,
  [property: JsonPropertyName("certificate")] string? Certificate = null
) {
  public string? OrderUrl { get; set; }
}

public record AcmeIdentifier(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("value")] string Value
);

public record AcmeAuthorization(
  [property: JsonPropertyName("status")] string Status,
  [property: JsonPropertyName("identifier")] AcmeIdentifier Identifier,
  [property: JsonPropertyName("challenges")] AcmeChallenge[] Challenges,
  [property: JsonPropertyName("expires")] DateTime? Expires = null
);

public record AcmeChallenge(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("status")] string Status,
  [property: JsonPropertyName("url")] string Url,
  [property: JsonPropertyName("token")] string Token,
  [property: JsonPropertyName("validated")] DateTime? Validated = null,
  [property: JsonPropertyName("error")] AcmeError? Error = null
);

public record AcmeError(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("detail")] string Detail,
  [property: JsonPropertyName("status")] int Status
);

public class AcmeException : Exception {
  public AcmeException(string message) : base(message) { }
  public AcmeException(string message, Exception innerException) : base(message, innerException) { }
}
