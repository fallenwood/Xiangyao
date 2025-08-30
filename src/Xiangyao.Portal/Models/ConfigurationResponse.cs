namespace Xiangyao.Api;

using ZLinq;

public record ConfigurationResponse(
  ProxyConfig ProxyConfig) {
  private const string Null = "<null>";
  public static ConfigurationResponse From(XiangyaoProxyConfig config) {
    var clusters = config.ProxyConfig.Clusters.AsValueEnumerable().Select(c => {
      var destination = c.Destinations?.Values.AsValueEnumerable().FirstOrDefault();
      string? unixSocketPath = null;

      _ = c.Metadata?.TryGetValue("UnixSocketPath", out unixSocketPath);

      return new Cluster(
        ClusterId: c.ClusterId,
        Destination: new Destination(
          Address: destination?.Address ?? Null,
          Health: destination?.Health,
          Host: destination?.Host),
        UnixSocketPath: unixSocketPath);
    }).ToArray();

    var cluserIdToCluster = clusters.AsValueEnumerable().ToDictionary(c => c.ClusterId, c => c);

    var routes = config.ProxyConfig.Routes.AsValueEnumerable().Select(r => {
      return new Route(
        RouteId: r.RouteId,
        Order: r.Order,
        ClusterId: r.ClusterId,
        Cluster: r.ClusterId != null ? cluserIdToCluster[r.ClusterId] : null,
        RouteMatch: new(
          Hosts: r.Match.Hosts?.AsEnumerable().ToArray(),
          Path: r.Match.Path));
      }).ToArray();

    return new ConfigurationResponse(
      ProxyConfig: new ProxyConfig(
        Provider: string.Empty,
        Clusters: clusters,
        Routes: routes));
  }
}

public record ProxyConfig(
  string Provider,
  Cluster[] Clusters,
  Route[] Routes);

public record Route(
  string RouteId,
  int? Order,
  string? ClusterId,
  Cluster? Cluster,
  RouteMatch RouteMatch);

public record RouteMatch(
  string[]? Hosts,
  string? Path);

public record Cluster(
  string ClusterId,
  Destination Destination,
  string? UnixSocketPath);

public record Destination(
  string Address,
  string? Health,
  string? Host);
