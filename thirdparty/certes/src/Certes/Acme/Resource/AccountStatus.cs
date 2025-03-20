namespace Certes.Acme.Resource;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the status of <see cref="Account"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccountStatus>))]
public enum AccountStatus
{
    /// <summary>
    /// The valid status.
    /// </summary>
    [EnumMember(Value = "valid")]
    Valid,

    /// <summary>
    /// The deactivated status, initiated by client.
    /// </summary>
    [EnumMember(Value = "deactivated")]
    Deactivated,

    /// <summary>
    /// The revoked status, initiated by server.
    /// </summary>
    [EnumMember(Value = "revoked")]
    Revoked,
}
