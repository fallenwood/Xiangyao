namespace Xiangyao.Benchmarks;

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

public class LabelParserBenchmarks {
  private ILabelParser switchCaseLabelParser = new SwitchCaseLabelParser();
  private ILabelParser stateMachineLabelParser = new StateMachineLabelParser();

  private readonly List<KeyValuePair<string, string>> labels = [
    new ("xiangyao.enable", "true"),
    new ("xiangyao.cluster.port", "80"),
    new ("xiangyao.cluster.schema", "http"),
    new ("xiangyao.routes.nginx_http.match.host", "localhost:5000"),
    new ("xiangyao.routes.nginx_http.match.path", "{**catch-all}"),
  ];

  private void Benchmark(ILabelParser labelParser) {
    var dict = new DefaultDictionary<string, RouteConfig>(capacity: labels.Count);

    foreach (var label in labels) {
      labelParser.Parse(label, dict);
    }
  }

  [Benchmark]
  public void SwitchCase() => Benchmark(switchCaseLabelParser);

  [Benchmark]
  public void StateMachine() => Benchmark(stateMachineLabelParser);
}
