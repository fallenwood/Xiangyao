namespace Xiangyao;

using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

internal class DockerProxyConfig(
  IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteConfig> routes,
  IReadOnlyList<ClusterConfig> clusters,
  CancellationToken cancellationToken)
  : IProxyConfig {

  public IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteConfig> Routes { get; } = routes;

  public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;

  public IChangeToken ChangeToken { get; } = new CancellationChangeToken(cancellationToken);
}
