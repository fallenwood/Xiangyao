﻿namespace Certes.Pkcs;

using System.IO;
using System.Text.Json.Serialization;
using Certes.Crypto;
using Certes.Properties;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

/// <summary>
/// Represents a key pair.
/// </summary>
public class KeyInfo
{
    /// <summary>
    /// Gets or sets the private key information.
    /// </summary>
    /// <value>
    /// The private key information.
    /// </value>
    [JsonPropertyName("der")]
    public byte[]? PrivateKeyInfo { get; set; }

    /// <summary>
    /// Reads the key from the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The steam.</param>
    /// <returns>The key loaded.</returns>
    public static KeyInfo From(Stream stream)
    {
        using (var streamReader = new StreamReader(stream))
        {
            var reader = new PemReader(streamReader);

            if (!(reader.ReadObject() is AsymmetricCipherKeyPair keyPair))
            {
                throw new AcmeException(Strings.ErrorInvalidKeyData);
            }

            return keyPair.Export();
        }
    }
}

/// <summary>
/// Helper methods for <see cref="KeyInfo"/>.
/// </summary>
public static class KeyInfoExtensions
{
    private static readonly KeyAlgorithmProvider keyAlgorithmProvider = new KeyAlgorithmProvider();

    /// <summary>
    /// Saves the key pair to the specified stream.
    /// </summary>
    /// <param name="keyInfo">The key information.</param>
    /// <param name="stream">The stream.</param>
    public static void Save(this KeyInfo keyInfo, Stream stream)
    {
        var keyPair = keyInfo.CreateKeyPair();
        using (var writer = new StreamWriter(stream))
        {
            var pemWriter = new PemWriter(writer);
            pemWriter.WriteObject(keyPair);
        }
    }

    /// <summary>
    /// Gets the key pair.
    /// </summary>
    /// <param name="keyInfo">The key data.</param>
    /// <returns>The key pair</returns>
    internal static AsymmetricCipherKeyPair CreateKeyPair(this KeyInfo keyInfo)
    {
        var (_, keyPair) = keyAlgorithmProvider.GetKeyPair(keyInfo.PrivateKeyInfo ?? []);
        return keyPair;
    }

    /// <summary>
    /// Exports the key pair.
    /// </summary>
    /// <param name="keyPair">The key pair.</param>
    /// <returns>The key data.</returns>
    internal static KeyInfo Export(this AsymmetricCipherKeyPair keyPair)
    {
        var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);

        return new KeyInfo
        {
            PrivateKeyInfo = privateKey.GetDerEncoded()
        };
    }
}
