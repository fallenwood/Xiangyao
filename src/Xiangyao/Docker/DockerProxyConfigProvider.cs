namespace Xiangyao;

using System.Threading;
using Docker.DotNet.Models;
using LettuceEncrypt;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using YRC = Yarp.ReverseProxy.Configuration;

public class ChangeNotifier {
  private CancellationTokenSource source = new();

  public CancellationTokenSource Source => this.source;

  public void Notify(Action action) {
    var source = Interlocked.Exchange(ref this.source, new CancellationTokenSource());

    action();

    source.Cancel();
  }

  public async Task NotifyAsync(Func<Task> action) {
    var source = Interlocked.Exchange(ref this.source, new CancellationTokenSource());

    await action();

    source.Cancel();
  }
}

internal sealed class DockerProxyConfigProvider : IProxyConfigProvider, IUpdateConfig {
  private const long Threshold = 60 * 1000;

  private readonly IDockerProvider dockerProvider;
  private readonly ILogger<DockerProxyConfigProvider> logger;
  private readonly ILabelParser labelParser;
  private readonly IThroutteEngine throutteEngine;
  private readonly ILettuceEncryptOptionsProvider lettuceEncryptOptionsProvider;
  private readonly ChangeNotifier notifier = new();

  private XiangyaoProxyConfig config;

  public DockerProxyConfigProvider(
        IDockerProvider dockerProvider,
        ILabelParser labelParser,
        IThroutteEngine throutteEngine,
        ILettuceEncryptOptionsProvider lettuceEncryptOptionsProvider,
        ILogger<DockerProxyConfigProvider> logger) {
    this.dockerProvider = dockerProvider;
    this.logger = logger;
    this.labelParser = labelParser;
    this.throutteEngine = throutteEngine;
    this.lettuceEncryptOptionsProvider = lettuceEncryptOptionsProvider;

    this.config = new(
      ProxyConfig: new DockerProxyConfig(
      [],
      [],
      notifier.Source.Token));
  }

  public async Task<XiangyaoProxyConfig> GetXiangyaoProxyConfig() {
    logger.LogDebug(nameof(GetXiangyaoProxyConfig));

    var client = this.dockerProvider.CreateDockerClient();

    var allContainers = await client.Containers.ListContainersAsync(new() {
      All = true,
    });

    if (logger.IsEnabled(LogLevel.Debug)) {
      foreach (var c in allContainers) {
        logger.LogDebug("Container {Id} {Name} {Status}", c.ID, c.Names.FirstOrDefault(), c.Status);
      }
    }

    var routes = new List<YRC.RouteConfig>(allContainers.Count);
    var clusters = new List<YRC.ClusterConfig>(allContainers.Count);

    foreach (var container in allContainers) {
      var labels = container
          .Labels
          .Where(e => e.Key.StartsWith(XiangyaoConstants.LabelKeyPrefix, StringComparison.OrdinalIgnoreCase))
          .ToList();

      var enabled = this.labelParser.ParseEnabled(labels);

      if (!enabled) {
        this.logger.LogInformation("Container {ContainerId} is not enabled", container.ID);
        continue;
      }

      var host = this.labelParser.ParseHost(container);

      if (string.IsNullOrEmpty(host)) {
        this.logger.LogInformation("No valid host found for {Name}", container.Names.FirstOrDefault());
        continue;
      }

      var containerRoutes = this.ParseRouterConfigs(container, labels);

      var port = this.labelParser.ParsePort(labels);

      var schema = this.labelParser.ParseSchema(labels);

      var address = $"{schema}://{host}:{port}";

      var cluster = new YRC.ClusterConfig {
        ClusterId = container.Names[0],
        Destinations = new Dictionary<string, YRC.DestinationConfig>(1) {
          {
            container.Names[0],
            new () {
              Address = address,
            }
          }
        },
      };

      clusters.Add(cluster);

      foreach (var route in containerRoutes) {
        logger.LogDebug("Adding routing for {RouteId} -> {ClusterId}", route.RouteId, route.ClusterId);
        routes.Add(route);
      }
    }

    return new(new DockerProxyConfig(routes, clusters, this.notifier.Source.Token));
  }

  public List<YRC.RouteConfig> ParseRouterConfigs(ContainerListResponse container, List<KeyValuePair<string, string>> labels) {
    var parsedLabels = this.labelParser.ParseRouteConfigs(labels);

    var clusterId = container.Names[0];

    var routes = parsedLabels
      .Select(kvp => {
        var c = kvp.Value;

        var config = new YRC.RouteConfig {
          RouteId = kvp.Key,
          Match = new YRC.RouteMatch {
            Hosts = c.Match.Hosts,
            Path = c.Match.Path,
          },
          ClusterId = clusterId,
        };

        return config.WithTransformUseOriginalHostHeader(useOriginal: true);
      })
      .ToList();

    return routes;
  }

  public IProxyConfig GetConfig() {
    logger.LogDebug(nameof(GetConfig));

    var config = this.config.ProxyConfig;

    logger.LogInformation("Current Config: {Routes} Routes, {Clusters} Clusters", config.Routes.Count, config.Clusters.Count);

    return config;
  }

  public async ValueTask UpdateAsync() {
    logger.LogDebug(nameof(UpdateAsync));

    var now = DateTimeOffset.UtcNow;

    var throttled = await this.throutteEngine.ThrottleAsync();

    if (this.logger.IsEnabled(LogLevel.Debug)) {
      this.logger.LogDebug("Throttled {Now}? {throttled}", now, throttled);
    }

    if (throttled) {
      return;
    }

    await this.notifier.NotifyAsync(async () => {
      var newConfig = await this.GetXiangyaoProxyConfig();

      Interlocked.Exchange(ref this.config, newConfig);

      var addresses = newConfig.ProxyConfig.Routes.SelectMany(e => e.Match.Hosts ?? []).Distinct().ToArray();

      this.lettuceEncryptOptionsProvider.SetDomainNames(addresses);

      if (logger.IsEnabled(LogLevel.Debug)) {
        logger.LogDebug("New Addresses {hosts}", string.Join(",", addresses));
      }
    });

    if (logger.IsEnabled(LogLevel.Debug)) {
      logger.LogDebug("New Configuration {Configuration}", System.Text.Json.JsonSerializer.Serialize(this.config));
    }
  }
}
