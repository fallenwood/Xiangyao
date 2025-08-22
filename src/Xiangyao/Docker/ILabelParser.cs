namespace Xiangyao;

using Xiangyao.Docker;
using ZLinq;

internal interface ILabelParser {
  public IReadOnlyDictionary<string, RouteConfig> ParseRouteConfigs(Label[] labels) {
    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Length);

    foreach (var label in labels) {
      _ = this.Parse(label, dict);
    }

    return dict.ToDictionary();
  }

  public bool ParseEnabled(Label[] labels) {
    var enabled = labels
      .AsValueEnumerable()
      .FirstOrDefault(e =>
        string.Equals(e.Name, XiangyaoConstants.EnableLabelKey, StringComparison.OrdinalIgnoreCase))
        ?.Value;

    if (string.IsNullOrEmpty(enabled)
      || !string.Equals(enabled, bool.TrueString, StringComparison.OrdinalIgnoreCase)) {
      return false;
    }

    return true;
  }

  public string ParseHost(ListContainerResponse container) {
    var fisrtNetwork = container.NetworkSettings?.AsValueEnumerable().FirstOrDefault();
    var host = fisrtNetwork?.IPAddress;

    return host ?? string.Empty;
  }

  public string ParseSchema(Label[] labels) {
    var schema = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == XiangyaoConstants.SchemaLabelKey)?.Value;

    if (string.IsNullOrEmpty(schema)) {
      schema = XiangyaoConstants.Http;
    }

    return schema;
  }

  public string? ParseCustomHost(Label[] labels) {
    var host = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == XiangyaoConstants.HostLabelKey)?.Value;
    return host;
  }

  public int ParsePort(Label[] labels) {
    var portString = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == XiangyaoConstants.PortLabelKey)?.Value;
    if (string.IsNullOrEmpty(portString) || !int.TryParse(portString, out var port)) {
      return XiangyaoConstants.HttpPort;
    }

    return port;
  }

  public string ParseSocketPath(Label[] labels) {
    var path = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == XiangyaoConstants.UnixSocketPathLabelKey)?.Value;
    return path ?? string.Empty;
  }

  public bool Parse(Label label, DefaultDictionary<string, RouteConfig> parsedLabels);
}
