namespace Certes.Acme.Resource;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

/// <summary>
/// Represents type of <see cref="Identifier"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<IdentifierType>))]
public enum IdentifierType
{
    /// <summary>
    /// The DNS type.
    /// </summary>
    [EnumMember(Value = "dns")]
    Dns,
}
