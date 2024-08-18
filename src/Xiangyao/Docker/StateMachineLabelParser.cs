namespace Xiangyao;

using Xiangyao.Docker;

internal sealed class StateMachineLabelParser : ILabelParser {
  public bool Parse(Label label, DefaultDictionary<string, RouteConfig> parsedLabels) {
    var stateMachine = new StateMachine(label.Name, label.Value);

    stateMachine.Parse(parsedLabels);

    return true;
  }

  public enum State {
    NotStarted,
    Processing,
    Tokenizied,
    Completed,
  }

  internal class StateMachine(string key, string value) {
    int index = 0;
    int tokenStart = 0;
    int tokenIndex = 0;
    State state = State.NotStarted;
    bool routes = false;
    bool match = false;
    string? routeName = null;

    // xiangyao.routes.nginx_http.match.host=localhost:5000
    public void Parse(DefaultDictionary<string, RouteConfig> parsedLabels) {
      while (true) {
        if (state == State.Completed) {
          break;
        }

        if (state == State.NotStarted) {
          index = 0;
          tokenStart = 0;
          state = State.Processing;
          continue;
        }

        if (state == State.Tokenizied) {
          var token = key[tokenStart..index];

          index++;
          tokenStart = index;

          if (tokenIndex == 0) {
            // NOOP
          } else if (tokenIndex == 1) {
            if (string.Equals(token, "routes", StringComparison.OrdinalIgnoreCase)) {
              routes = true;
            }
          }

          if (routes) {
            if (tokenIndex == 2) {
              routeName = token;
            } else if (tokenIndex == 3) {
              if (string.Equals(token, "match", StringComparison.OrdinalIgnoreCase)) {
                match = true;
              }
            }

            if (match) {
              if (tokenIndex == 4) {
                var route = parsedLabels[routeName!]!;

                switch (token) {
                  case "host": {
                      route.Match.Hosts.Add(value);
                      break;
                    }
                  case "hosts": {
                      var hosts = value.Split(';');
                      route.Match.Hosts = [.. hosts];
                      break;
                    }
                  case "path": {
                      route.Match.Path = value;
                      break;
                    }
                  default:
                    break;
                }
              }
            }
          }

          tokenIndex++;

          if (index >= key.Length) {
            state = State.Completed;
          } else {
            state = State.Processing;
          }

          continue;
        }

        if (state == State.Processing) {
          if (index >= key.Length || key[index] == '.') {
            state = State.Tokenizied;
            continue;
          }

          index++;
          continue;
        }
      }
    }
  }

}
