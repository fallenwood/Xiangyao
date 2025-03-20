﻿namespace Certes.Jws;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a JWK using RSA.
/// </summary>
/// <seealso cref="Certes.Jws.JsonWebKey" />
public class RsaJsonWebKey : JsonWebKey
{
    /// <summary>
    /// Gets or sets the exponent value for the RSA public key.
    /// </summary>
    /// <value>
    /// The exponent value for the RSA public key.
    /// </value>
    [JsonPropertyName("e")]
    [JsonPropertyOrder(1)]
    public string? Exponent { get; set; }

    /// <summary>
    /// Gets or sets the modulus value for the RSA public key.
    /// </summary>
    /// <value>
    /// The modulus value for the RSA public key.
    /// </value>
    [JsonPropertyName("n")]
    [JsonPropertyOrder(3)]
    public string? Modulus { get; set; }
}
