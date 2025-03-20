namespace Certes.Acme.Resource;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the identifier for <see cref="Authorization"/>.
/// </summary>
public class Identifier
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    [JsonPropertyName("type")]
    public IdentifierType? Type { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
