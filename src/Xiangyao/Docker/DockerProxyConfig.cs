namespace Xiangyao;

using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

using YRC = Yarp.ReverseProxy.Configuration;

internal class DockerProxyConfig(
  IReadOnlyList<YRC.RouteConfig> routes,
  IReadOnlyList<ClusterConfig> clusters,
  CancellationToken cancellationToken)
  : IProxyConfig {

  public IReadOnlyList<YRC.RouteConfig> Routes { get; } = routes;

  public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;

  public IChangeToken ChangeToken { get; } = new CancellationChangeToken(cancellationToken);
}
