﻿namespace Certes.Acme.Resource;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the payload for certificate revocation.
/// </summary>
public class CertificateRevocation
{
    /// <summary>
    /// Gets or sets the certificate to be revoked, in the base64url-encoded version of the DER format..
    /// </summary>
    /// <value>
    /// The certificate to be revoked, in the base64url-encoded version of the DER format.
    /// </value>
    [JsonPropertyName("certificate")]
    public string? Certificate { get; set; }


    /// <summary>
    /// Gets or sets the revocation reason.
    /// </summary>
    /// <value>
    /// The revocation reason.
    /// </value>
    [JsonPropertyName("reason")]
    public RevocationReason? Reason { get; set; }
}
