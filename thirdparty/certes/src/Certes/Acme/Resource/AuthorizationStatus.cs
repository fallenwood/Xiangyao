﻿namespace Certes.Acme.Resource;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the status of <see cref="Authorization"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AuthorizationStatus>))]
public enum AuthorizationStatus
{
    /// <summary>
    /// The pending status.
    /// </summary>
    [EnumMember(Value = "pending")]
    Pending,

    /// <summary>
    /// The valid status.
    /// </summary>
    [EnumMember(Value = "valid")]
    Valid,

    /// <summary>
    /// The invalid status.
    /// </summary>
    [EnumMember(Value = "invalid")]
    Invalid,

    /// <summary>
    /// The revoked status.
    /// </summary>
    [EnumMember(Value = "revoked")]
    Revoked,

    /// <summary>
    /// The deactivated status.
    /// </summary>
    [EnumMember(Value = "deactivated")]
    Deactivated,

    /// <summary>
    /// The expired status.
    /// </summary>
    [EnumMember(Value = "expired")]
    Expired,
}
