namespace Xiangyao.Acme;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ZeroSslExternalAccountBindingProvider(HttpClient httpClient) {
  public const string Endpoint = "https://api.zerossl.com/acme/eab-credentials-email";

  public async Task<AcmeExternalAccountBindingOptions> GetOrCreateAsync(
    string email,
    string cachePath,
    CancellationToken cancellationToken = default) {
    var cachedOptions = Load(cachePath);
    if (cachedOptions != null) {
      return cachedOptions;
    }

    var options = await this.CreateAsync(email, cancellationToken);
    Save(cachePath, options);
    return options;
  }

  public async Task<AcmeExternalAccountBindingOptions> CreateAsync(
    string email,
    CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(email)) {
      throw new AcmeException("ZeroSSL requires an account email address to obtain EAB credentials automatically.");
    }

    using var content = new FormUrlEncodedContent([
      new KeyValuePair<string, string>("email", email),
    ]);

    using var response = await httpClient.PostAsync(Endpoint, content, cancellationToken);
    if (!response.IsSuccessStatusCode) {
      var error = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new AcmeException($"Failed to obtain ZeroSSL EAB credentials: {response.StatusCode} - {error}");
    }

    var eabResponse = await response.Content.ReadFromJsonAsync(
      AcmeJsonContext.Default.ZeroSslExternalAccountBindingResponse,
      cancellationToken);

    if (eabResponse == null ||
      string.IsNullOrWhiteSpace(eabResponse.EabKid) ||
      string.IsNullOrWhiteSpace(eabResponse.EabHmacKey)) {
      throw new AcmeException("ZeroSSL EAB response did not include eab_kid and eab_hmac_key.");
    }

    return new AcmeExternalAccountBindingOptions(eabResponse.EabKid, eabResponse.EabHmacKey);
  }

  public static AcmeExternalAccountBindingOptions? Load(string cachePath) {
    if (!File.Exists(cachePath)) {
      return null;
    }

    using var stream = File.OpenRead(cachePath);
    var options = JsonSerializer.Deserialize(stream, AcmeJsonContext.Default.AcmeExternalAccountBindingOptions);
    if (options == null ||
      string.IsNullOrWhiteSpace(options.KeyIdentifier) ||
      string.IsNullOrWhiteSpace(options.HmacKey)) {
      throw new AcmeException($"ZeroSSL EAB cache file is invalid: {cachePath}");
    }

    return options;
  }

  public static void Save(string cachePath, AcmeExternalAccountBindingOptions options) {
    var directory = Path.GetDirectoryName(cachePath);
    if (!string.IsNullOrEmpty(directory)) {
      Directory.CreateDirectory(directory);
    }

    using var stream = File.Create(cachePath);
    JsonSerializer.Serialize(stream, options, AcmeJsonContext.Default.AcmeExternalAccountBindingOptions);
  }
}

internal sealed record ZeroSslExternalAccountBindingResponse(
  [property: JsonPropertyName("eab_kid")] string? EabKid,
  [property: JsonPropertyName("eab_hmac_key")] string? EabHmacKey);
