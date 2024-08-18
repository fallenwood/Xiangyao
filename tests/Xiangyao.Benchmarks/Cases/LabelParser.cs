namespace Xiangyao.Benchmarks;

using BenchmarkDotNet.Attributes;
using Xiangyao.Docker;

public class LabelParserBenchmarks {
  private ILabelParser switchCaseLabelParser = new SwitchCaseLabelParser();
  private ILabelParser stateMachineLabelParser = new StateMachineLabelParser();

  private readonly Label[] labels = [
    new Label { Name = "xiangyao.enable",  Value = "true" },
    new Label{ Name = "xiangyao.cluster.port", Value =  "80" },
    new Label{ Name = "xiangyao.cluster.schema",  Value = "http" },
    new Label{ Name = "xiangyao.routes.nginx_http.match.host", Value =  "localhost:5000" },
    new Label{ Name = "xiangyao.routes.nginx_http.match.path", Value =  "{**catch-all}" },
  ];

  private void Benchmark(ILabelParser labelParser) {
    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Length);

    foreach (var label in labels) {
      labelParser.Parse(label, dict);
    }
  }

  [Benchmark]
  public void SwitchCase() => Benchmark(switchCaseLabelParser);

  [Benchmark]
  public void StateMachine() => Benchmark(stateMachineLabelParser);
}
