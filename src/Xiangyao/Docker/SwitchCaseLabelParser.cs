namespace Xiangyao;

internal sealed class SwitchCaseLabelParser : ILabelParser {
  public bool Parse(KeyValuePair<string, string> label, DefaultDictionary<string, RouteConfig> parsedLabels) {
    var keys = label.Key.Split('.');

    if (keys.Length < 3) {
      return false;
    }

    switch (keys[1]) {
      case "routes": {
          var routeName = keys[2];
          if (keys.Length < 4) {
            break;
          }

          var section = keys[3];

          if (string.Compare(section, "match", StringComparison.OrdinalIgnoreCase) != 0) {
            break;
          }

          var property = keys[4];

          var route = parsedLabels[routeName]!;

          switch (section) {
            case "match": {
                switch (property) {
                  case "host": {
                      route.Match.Hosts.Add(label.Value);
                      break;
                    }
                  case "hosts": {
                      var hosts = label.Value.Split(';');
                      route.Match.Hosts = [.. hosts];
                      break;
                    }
                  case "path": {
                      route.Match.Path = label.Value;
                      break;
                    }
                  default:
                    break;
                }
                break;
              }
            default:
              break;
          }

          break;
        }
      default:
        break;

    }

    return true;
  }
}
