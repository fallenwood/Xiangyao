namespace Certes.Jws;

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Certes.Acme.Models;

/// <summary>
/// Represents an signer for JSON Web Signature.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JwsSigner"/> class.
/// </remarks>
/// <param name="keyPair">The keyPair.</param>
internal class JwsSigner(IKey keyPair)
{
    /// <summary>
    /// Signs the specified payload.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="nonce">The nonce.</param>
    /// <param name="jsonTypeInfo">The JSON type information.</param>
    /// <returns>The signed payload.</returns>
    public JwsPayload Sign<T>(T payload, string nonce, JsonTypeInfo<T> jsonTypeInfo)
        => Sign(payload, jsonTypeInfo, null, null, nonce);

    /// <summary>
    /// Encodes this instance.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="jsonTypeInfo">The JSON type information.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="url">The URL.</param>
    /// <param name="nonce">The nonce.</param>
    /// <returns>The signed payload.</returns>
    public JwsPayload Sign<T>(
        T payload,
        JsonTypeInfo<T> jsonTypeInfo,
        Uri? keyId = null,
        Uri? url = null,
        string? nonce = null)
    {
        // var jsonSettings = JsonUtil.CreateSettings();
        ProtectedHeader protectedHeader = (keyId) == null ?
            new(
                alg: keyPair.Algorithm.ToJwsAlgorithm(),
                jwk: keyPair.JsonWebKey,
                nonce: nonce,
                url: url)
            :
            new(
                alg: keyPair.Algorithm.ToJwsAlgorithm(),
                nonce: nonce,
                url: url,
                kid: keyId);

        var entityJson = payload == null ?
            string.Empty :
            // JsonConvert.SerializeObject(payload, Formatting.None, jsonSettings);
            JsonSerializer.Serialize(payload, jsonTypeInfo);
        // var protectedHeaderJson = JsonConvert.SerializeObject(protectedHeader, Formatting.None, jsonSettings);

        var protectedHeaderJson = JsonSerializer.Serialize(protectedHeader, AcmeJsonContext.Default.ProtectedHeader);

        var payloadEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(entityJson));
        var protectedHeaderEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(protectedHeaderJson));

        var signature = $"{protectedHeaderEncoded}.{payloadEncoded}";
        var signatureBytes = Encoding.UTF8.GetBytes(signature);
        var signedSignatureBytes = keyPair.GetSigner().SignData(signatureBytes);
        var signedSignatureEncoded = JwsConvert.ToBase64String(signedSignatureBytes);

        var body = new JwsPayload
        {
            Protected = protectedHeaderEncoded,
            Payload = payloadEncoded,
            Signature = signedSignatureEncoded
        };

        return body;
    }
}
