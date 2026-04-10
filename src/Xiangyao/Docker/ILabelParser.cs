namespace Xiangyao;

using Xiangyao.Docker;

internal interface ILabelParser {
  public IReadOnlyDictionary<string, RouteConfig> ParseRouteConfigs(Label[] labels) {
    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Length);

    foreach (var label in labels) {
      if (!label.Name.StartsWith(XiangyaoConstants.RoutesLabelKeyPrefix, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      _ = this.Parse(label, dict);
    }

    return dict.ToDictionary();
  }

  public bool ParseEnabled(Label[] labels) {
    foreach (var label in labels) {
      if (string.Equals(label.Name, XiangyaoConstants.EnableLabelKey, StringComparison.OrdinalIgnoreCase)) {
        return string.Equals(label.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
      }
    }

    return false;
  }

  public string ParseHost(ListContainerResponse container) {
    if (container.NetworkSettings.Length == 0) {
      return string.Empty;
    }

    return container.NetworkSettings[0].IPAddress ?? string.Empty;
  }

  public string ParseSchema(Label[] labels) {
    foreach (var label in labels) {
      if (string.Equals(label.Name, XiangyaoConstants.SchemaLabelKey, StringComparison.OrdinalIgnoreCase)) {
        return string.IsNullOrEmpty(label.Value) ? XiangyaoConstants.Http : label.Value;
      }
    }

    return XiangyaoConstants.Http;
  }

  public string? ParseCustomHost(Label[] labels) {
    foreach (var label in labels) {
      if (string.Equals(label.Name, XiangyaoConstants.HostLabelKey, StringComparison.OrdinalIgnoreCase)) {
        return label.Value;
      }
    }

    return null;
  }

  public int ParsePort(Label[] labels) {
    foreach (var label in labels) {
      if (string.Equals(label.Name, XiangyaoConstants.PortLabelKey, StringComparison.OrdinalIgnoreCase)) {
        return int.TryParse(label.Value, out var port)
          ? port
          : XiangyaoConstants.HttpPort;
      }
    }

    return XiangyaoConstants.HttpPort;
  }

  public string ParseSocketPath(Label[] labels) {
    foreach (var label in labels) {
      if (string.Equals(label.Name, XiangyaoConstants.UnixSocketPathLabelKey, StringComparison.OrdinalIgnoreCase)) {
        return label.Value ?? string.Empty;
      }
    }

    return string.Empty;
  }

  public bool Parse(Label label, DefaultDictionary<string, RouteConfig> parsedLabels);
}
