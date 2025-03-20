namespace Certes.Acme.Resource;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the ACME directory resource.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Directory"/> class.
/// </remarks>
/// <param name="newNonce">The new nonce.</param>
/// <param name="newAccount">The new account.</param>
/// <param name="newOrder">The new order.</param>
/// <param name="revokeCert">The revoke cert.</param>
/// <param name="keyChange">The key change.</param>
/// <param name="meta">The meta.</param>
public class Directory(
    Uri newNonce,
    Uri newAccount,
    Uri newOrder,
    Uri revokeCert,
    Uri keyChange,
    DirectoryMeta meta)
{
    /// <summary>
    /// Gets or sets the new nonce endpoint.
    /// </summary>
    /// <value>
    /// The new nonce endpoint.
    /// </value>
    [JsonPropertyName("newNonce")]
    public Uri NewNonce { get; } = newNonce;

    /// <summary>
    /// Gets or sets the new account endpoint.
    /// </summary>
    /// <value>
    /// The new account endpoint.
    /// </value>
    [JsonPropertyName("newAccount")]
    public Uri NewAccount { get; } = newAccount;

    /// <summary>
    /// Gets or sets the new order endpoint.
    /// </summary>
    /// <value>
    /// The new order endpoint.
    /// </value>
    [JsonPropertyName("newOrder")]
    public Uri NewOrder { get; } = newOrder;

    /// <summary>
    /// Gets or sets the revoke cert.
    /// </summary>
    /// <value>
    /// The revoke cert.
    /// </value>
    [JsonPropertyName("revokeCert")]
    public Uri RevokeCert { get; } = revokeCert;

    /// <summary>
    /// Gets or sets the key change endpoint.
    /// </summary>
    /// <value>
    /// The key change endpoint.
    /// </value>
    [JsonPropertyName("keyChange")]
    public Uri KeyChange { get; } = keyChange;

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    /// <value>
    /// The metadata.
    /// </value>
    [JsonPropertyName("meta")]
    public DirectoryMeta Meta { get; } = meta;
}
