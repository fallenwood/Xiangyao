namespace Xiangyao;

using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

internal class DockerProxyConfig : IProxyConfig {
  public DockerProxyConfig(
    IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters,
    CancellationToken cancellationToken) {
    this.Routes = routes;
    this.Clusters = clusters;
    this.ChangeToken = new CancellationChangeToken(cancellationToken);
  }

  public IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteConfig> Routes { get; }

  public IReadOnlyList<ClusterConfig> Clusters { get; }

  public IChangeToken ChangeToken { get; }
}
