namespace Xiangyao.UnitTests.Acme;

using System.Net;
using System.Text;
using System.Text.Json;
using Xiangyao.Acme;

public sealed class AcmeClientTests {
  [Fact]
  public void GetDirectoryUrl_ReturnsExpectedDirectory() {
    AcmeDirectoryUrls.GetDirectoryUrl(AcmeCertificateAuthority.LetsEncrypt).Should().Be(AcmeDirectoryUrls.LetsEncrypt);
    AcmeDirectoryUrls.GetDirectoryUrl(AcmeCertificateAuthority.ZeroSsl).Should().Be(AcmeDirectoryUrls.ZeroSsl);
  }

  [Fact]
  public void CreateAccountAsync_WithExternalAccountBinding_AddsEabJws() {
    using var handler = new AcmeTestHandler();
    using var httpClient = new HttpClient(handler);
    using var client = new AcmeClient(httpClient, AcmeTestHandler.DirectoryUrl);
    var hmacKey = Base64UrlEncode(Encoding.UTF8.GetBytes("shared-secret"));

#pragma warning disable xUnit1031
    client.InitializeAsync().GetAwaiter().GetResult();
    client.CreateAccountAsync(
      ["test@example.com"],
      termsOfServiceAgreed: true,
      new AcmeExternalAccountBindingOptions("kid-123", hmacKey)).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

    handler.AccountRequestBody.Should().NotBeNull();
    using var accountEnvelope = JsonDocument.Parse(handler.AccountRequestBody!);
    var accountPayload = DecodeBase64UrlToString(accountEnvelope.RootElement.GetProperty("payload").GetString()!);
    using var accountPayloadDocument = JsonDocument.Parse(accountPayload);
    var externalAccountBinding = accountPayloadDocument.RootElement.GetProperty("externalAccountBinding");

    externalAccountBinding.GetProperty("signature").GetString().Should().NotBeNullOrWhiteSpace();
    DecodeBase64UrlToString(externalAccountBinding.GetProperty("payload").GetString()!)
      .Should().Contain("\"kty\":\"RSA\"");

    using var protectedHeaderDocument = JsonDocument.Parse(
      DecodeBase64UrlToString(externalAccountBinding.GetProperty("protected").GetString()!));
    var protectedHeader = protectedHeaderDocument.RootElement;

    protectedHeader.GetProperty("alg").GetString().Should().Be("HS256");
    protectedHeader.GetProperty("kid").GetString().Should().Be("kid-123");
    protectedHeader.GetProperty("url").GetString().Should().Be(AcmeTestHandler.AccountUrl);
  }

  [Fact]
  public void GetOrCreateAsync_WithoutCachedCredentials_ObtainsAndCachesEabCredentials() {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var cachePath = Path.Combine(directory, "zerossl-eab.json");

    try {
      using var handler = new AcmeTestHandler();
      using var httpClient = new HttpClient(handler);
      var provider = new ZeroSslExternalAccountBindingProvider(httpClient);

#pragma warning disable xUnit1031
      var options = provider.GetOrCreateAsync("test@example.com", cachePath).GetAwaiter().GetResult();
      var cachedOptions = provider.GetOrCreateAsync("other@example.com", cachePath).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

      options.KeyIdentifier.Should().Be("generated-kid");
      options.HmacKey.Should().Be("generated-hmac-key");
      cachedOptions.Should().Be(options);
      handler.EabRequestCount.Should().Be(1);
      handler.EabRequestBody.Should().Be("email=test%40example.com");
      File.Exists(cachePath).Should().BeTrue();
    } finally {
      if (Directory.Exists(directory)) {
        Directory.Delete(directory, recursive: true);
      }
    }
  }

  private static string DecodeBase64UrlToString(string data) {
    return Encoding.UTF8.GetString(DecodeBase64Url(data));
  }

  private static byte[] DecodeBase64Url(string data) {
    var base64 = data
      .Replace('-', '+')
      .Replace('_', '/');

    var padding = base64.Length % 4;
    if (padding > 0) {
      base64 = base64.PadRight(base64.Length + 4 - padding, '=');
    }

    return Convert.FromBase64String(base64);
  }

  private static string Base64UrlEncode(byte[] data) {
    return Convert.ToBase64String(data)
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '_');
  }

  private sealed class AcmeTestHandler : HttpMessageHandler {
    public const string DirectoryUrl = "https://acme.test/directory";
    public const string NonceUrl = "https://acme.test/new-nonce";
    public const string AccountUrl = "https://acme.test/new-account";
    private const string NewOrderUrl = "https://acme.test/new-order";

    public string? AccountRequestBody { get; private set; }
    public int EabRequestCount { get; private set; }
    public string? EabRequestBody { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
      if (request.Method == HttpMethod.Post && request.RequestUri?.AbsoluteUri == ZeroSslExternalAccountBindingProvider.Endpoint) {
        this.EabRequestCount++;
        this.EabRequestBody = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

        return Task.FromResult(CreateJsonResponse(
          """
          {
            "eab_kid": "generated-kid",
            "eab_hmac_key": "generated-hmac-key"
          }
          """));
      }

      if (request.Method == HttpMethod.Get && request.RequestUri?.AbsoluteUri == DirectoryUrl) {
        return Task.FromResult(CreateJsonResponse(
          $$"""
          {
            "newNonce": "{{NonceUrl}}",
            "newAccount": "{{AccountUrl}}",
            "newOrder": "{{NewOrderUrl}}"
          }
          """));
      }

      if (request.Method == HttpMethod.Head && request.RequestUri?.AbsoluteUri == NonceUrl) {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("Replay-Nonce", "nonce-1");
        return Task.FromResult(response);
      }

      if (request.Method == HttpMethod.Post && request.RequestUri?.AbsoluteUri == AccountUrl) {
        this.AccountRequestBody = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

        var response = CreateJsonResponse(
          """
          {
            "status": "valid",
            "contact": [ "mailto:test@example.com" ]
          }
          """,
          HttpStatusCode.Created);
        response.Headers.Location = new Uri("https://acme.test/account/1");
        response.Headers.Add("Replay-Nonce", "nonce-2");
        return Task.FromResult(response);
      }

      throw new InvalidOperationException($"Unexpected ACME test request: {request.Method} {request.RequestUri}");
    }

    private static HttpResponseMessage CreateJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) {
      return new HttpResponseMessage(statusCode) {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
      };
    }
  }
}
