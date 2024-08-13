using Xiangyao.Docker;

namespace Xiangyao.UnitTests.Docker;

public class LabelParserTests {
  private readonly List<KeyValuePair<string, string>> labels = [
    new ("xiangyao.enable", "true"),
    new ("xiangyao.cluster.port", "80"),
    new ("xiangyao.cluster.schema", "http"),
    new ("xiangyao.routes.nginx_http.match.host", "localhost:5000"),
    new ("xiangyao.routes.nginx_http.match.path", "{**catch-all}"),
  ];

  private readonly ListContainerResponse emptyContainerListResponse = new() {
  };

  private readonly ListContainerResponse containerListResponse = new() {
    // NetworkSettings = new SummaryNetworkSettings {
    //   Networks = new Dictionary<string, EndpointSettings> {
    //     {
    //       "bridge",
    //       new EndpointSettings {
    //         Aliases = new List<string> {
    //           "nginx"
    //         }
    //       }
    //     }
    //   }
    // }
  };

  [Theory]
  [InlineData(typeof(StateMachineLabelParser))]
  [InlineData(typeof(SwitchCaseLabelParser))]
  public void Test_Parse_1_Label(Type parserType) {
    var parser = Activator.CreateInstance(parserType) as ILabelParser;

    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Count);

    foreach (var label in labels) {
      parser!.Parse(label, dict);
    }

    dict.Count.Should().Be(1);
    var value = dict["nginx_http"];

    value.Should().NotBeNull();
    value!.Match.Hosts.Should().Contain("localhost:5000");
    value!.Match.Path.Should().Be("{**catch-all}");
  }

  [Fact]
  public void Test_Parse_Default() {
    ILabelParser parser = new NoopLabelParser();

    parser.ParseEnabled(labels).Should().BeTrue();

    parser.ParsePort(labels).Should().Be(80);
    parser.ParseSchema(labels).Should().Be("http");

    parser.ParseHost(emptyContainerListResponse).Should().BeNullOrEmpty();

    parser.ParseHost(containerListResponse).Should().Be("nginx");
  }

  internal class NoopLabelParser : ILabelParser {
    public bool Parse(KeyValuePair<string, string> label, DefaultDictionary<string, RouteConfig> parsedLabels) {
      return false;
    }
  }
}
