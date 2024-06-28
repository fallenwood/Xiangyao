namespace Xiangyao;

internal class RouteMatch {
  public List<string> Hosts { get; set; } = new();
  public string Path { get; set; } = string.Empty;
}
