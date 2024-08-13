namespace Xiangyao;

using Xiangyao.Docker;

internal interface ILabelParser {
  public IReadOnlyDictionary<string, RouteConfig> ParseRouteConfigs(List<KeyValuePair<string, string>> labels) {
    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Count);

    foreach (var label in labels) {
      _ = this.Parse(label, dict);
    }

    return dict.ToDictionary();
  }

  public bool ParseEnabled(List<KeyValuePair<string, string>> labels) {
    var enabled = labels
      .FirstOrDefault(e =>
        string.Equals(e.Key, XiangyaoConstants.EnableLabelKey, StringComparison.OrdinalIgnoreCase))
      .Value;

    if (string.IsNullOrEmpty(enabled)
      || !string.Equals(enabled, bool.TrueString, StringComparison.OrdinalIgnoreCase)) {
      return false;
    }

    return true;
  }

  public string ParseHost(ListContainerResponse container) {
    // TODO
    return string.Empty;
    //var fisrtNetwork = container.NetworkSettings?.Networks?.Values.First();
    //
    //var host = fisrtNetwork?.Aliases?.FirstOrDefault();
    //
    //if (string.IsNullOrEmpty(host)) {
    //  host = fisrtNetwork?.IPAddress;
    //}
    //
    //return host ?? string.Empty;
  }

  public string ParseSchema(List<KeyValuePair<string, string>> labels) {
    var schema = labels.FirstOrDefault(e => e.Key == XiangyaoConstants.SchemaLabelKey).Value;

    if (string.IsNullOrEmpty(schema)) {
      schema = XiangyaoConstants.Http;
    }

    return schema;
  }

  public int ParsePort(List<KeyValuePair<string, string>> labels) {
    var portString = labels.FirstOrDefault(e => e.Key == XiangyaoConstants.PortLabelKey).Value;
    if (string.IsNullOrEmpty(portString) || !int.TryParse(portString, out var port)) {
      return XiangyaoConstants.HttpPort;
    }

    return port;
  }

  public bool Parse(KeyValuePair<string, string> label, DefaultDictionary<string, RouteConfig> parsedLabels);
}
