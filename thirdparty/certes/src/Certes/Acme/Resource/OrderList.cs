﻿namespace Certes.Acme.Resource;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the ACME Orders List resource.
/// </summary>
/// <remarks>
/// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.2.1
/// </remarks>
public class OrderList
{
    /// <summary>
    /// Gets or sets the orders.
    /// </summary>
    /// <value>
    /// The orders.
    /// </value>
    [JsonPropertyName("orders")]
    public IList<Uri>? Orders { get; set; }
}
