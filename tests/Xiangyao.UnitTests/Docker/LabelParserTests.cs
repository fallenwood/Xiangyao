using Xiangyao.Docker;

namespace Xiangyao.UnitTests.Docker;

public class LabelParserTests {
  private readonly Label[] labels = [
    new Label { Name = "xiangyao.enable",  Value = "true" },
    new Label{ Name = "xiangyao.cluster.port", Value =  "80" },
    new Label{ Name = "xiangyao.cluster.schema",  Value = "http" },
    new Label{ Name = "xiangyao.routes.nginx_http.match.host", Value =  "localhost:5000" },
    new Label{ Name = "xiangyao.routes.nginx_http.match.path", Value =  "{**catch-all}" },
  ];

  private readonly ListContainerResponse emptyContainerListResponse = new() {
  };

  private readonly ListContainerResponse containerListResponse = new() {
    NetworkSettings = [
      new NetworkEntry {
        Name = "nginx",
        IPAddress = "192.168.1.1",
        GlobalIPv6Address = "",
      }
    ],
  };

  [Theory]
  [InlineData(typeof(StateMachineLabelParser))]
  [InlineData(typeof(SwitchCaseLabelParser))]
  public void Test_Parse_1_Label(Type parserType) {
    var parser = Activator.CreateInstance(parserType) as ILabelParser;

    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Length);

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

    parser.ParseHost(containerListResponse).Should().Be("192.168.1.1");
  }

  internal class NoopLabelParser : ILabelParser {
    public bool Parse(Label label, DefaultDictionary<string, RouteConfig> parsedLabels) {
      return false;
    }
  }
}
