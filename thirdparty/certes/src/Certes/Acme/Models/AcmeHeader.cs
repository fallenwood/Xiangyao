namespace Certes.Acme.Models;

using System;
using System.Text.Json.Serialization;
using Certes.Jws;

/// <summary>
/// 
/// </summary>
public class AcmeHeader
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("alg")]
    public string? Alg { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("kid")]
    public string? Kid { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; set; }
}

/// <summary>
/// 
/// </summary>
/// <param name="alg"></param>
/// <param name="jwk"></param>
/// <param name="nonce"></param>
/// <param name="url"></param>
/// <param name="kid"></param>
public class ProtectedHeader(string alg, JsonWebKey? jwk, string? nonce, Uri? url, Uri? kid)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="alg"></param>
    /// <param name="nonce"></param>
    /// <param name="url"></param>
    /// <param name="kid"></param>
    public ProtectedHeader(string alg, string? nonce, Uri? url, Uri? kid)
        :this (alg, null, nonce, url, kid) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="alg"></param>
    /// <param name="jwk"></param>
    /// <param name="nonce"></param>
    /// <param name="url"></param>
    public ProtectedHeader(string alg, JsonWebKey jwk, string? nonce, Uri? url)
        : this(alg, jwk, nonce, url, null) { }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("alg")]
    public string Alg { get; set; } = alg;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("jwk")]
    public JsonWebKey? Jwk { get; set; } = jwk;
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; } = nonce;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; set; } = url;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("kid")]
    public Uri? Kid { get; set; } = kid;
}
