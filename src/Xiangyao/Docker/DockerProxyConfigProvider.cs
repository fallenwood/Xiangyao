namespace Xiangyao;

using System.Threading;
using LettuceEncrypt;
using Xiangyao.Docker;
using Xiangyao.Telemetry;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using YRC = Yarp.ReverseProxy.Configuration;

internal sealed class DockerProxyConfigProvider : IXiangyaoProxyConfigProvider {
  private const long Threshold = 60 * 1000;

  private readonly IDockerProvider dockerProvider;
  private readonly ILogger<DockerProxyConfigProvider> logger;
  private readonly ILabelParser labelParser;
  private readonly ILettuceEncryptOptionsProvider lettuceEncryptOptionsProvider;
  private readonly OpenTelemetryMeterProvider? meterProvider;

  private XiangyaoProxyConfig config;

  public DockerProxyConfigProvider(
        IDockerProvider dockerProvider,
        ILabelParser labelParser,
        ILettuceEncryptOptionsProvider lettuceEncryptOptionsProvider,
        ILogger<DockerProxyConfigProvider> logger,
        IServiceProvider serviceProvider) {
    this.dockerProvider = dockerProvider;
    this.logger = logger;
    this.labelParser = labelParser;
    this.lettuceEncryptOptionsProvider = lettuceEncryptOptionsProvider;
    this.meterProvider = serviceProvider.GetService<OpenTelemetryMeterProvider>();
    this.Notifier = new ChangeNotifier(this.UpdateImplAsync);

    this.config = new(
      ProxyConfig: new DockerProxyConfig(
      [],
      [],
      this.Notifier.Source.Token));
  }

  public XiangyaoProxyConfig Config => config;

  public IChangeNotifier Notifier { get; private set; }

  public async Task<XiangyaoProxyConfig> GetXiangyaoProxyConfigAsync() {
    logger.LogDebug(nameof(GetXiangyaoProxyConfigAsync));

    var client = this.dockerProvider.DockerClient;

    var allContainers = await client.ListContainersAsync();

    if (logger.IsEnabled(LogLevel.Debug)) {
      foreach (var c in allContainers) {
        logger.LogDebug("Container {Id} {Name} {Status}", c.Id, c.Names.FirstOrDefault(), c.Status);
      }
    }

    var routes = new List<YRC.RouteConfig>(allContainers.Length);
    var clusters = new List<YRC.ClusterConfig>(allContainers.Length);

    foreach (var container in allContainers) {
      var labels = container
          .Labels
          .Where(e => e.Name.StartsWith(XiangyaoConstants.LabelKeyPrefix, StringComparison.OrdinalIgnoreCase))
          .ToArray();

      var enabled = this.labelParser.ParseEnabled(labels);

      if (!enabled) {
        this.logger.LogInformation("Container {ContainerId} is not enabled", container.Id);
        this.meterProvider?.RecordDockerMiss();
        continue;
      }

      this.meterProvider?.RecordDockerHit();

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

    return new(new DockerProxyConfig(routes, clusters, this.Notifier.Source.Token));
  }

  public List<YRC.RouteConfig> ParseRouterConfigs(ListContainerResponse container, Label[] labels) {
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

  private async ValueTask UpdateImplAsync() {
    var newConfig = await this.GetXiangyaoProxyConfigAsync();

    Interlocked.Exchange(ref this.config, newConfig);

    var addresses = newConfig.ProxyConfig.Routes.SelectMany(e => e.Match.Hosts ?? []).Distinct().ToArray();

    this.lettuceEncryptOptionsProvider.SetDomainNames(addresses);

    if (logger.IsEnabled(LogLevel.Debug)) {
      logger.LogDebug("New Addresses {hosts}", string.Join(",", addresses));
      logger.LogDebug("New Configuration {Configuration}", System.Text.Json.JsonSerializer.Serialize(this.config));
    }
  }

  public void Update() {
    logger.LogDebug(nameof(Update));

    this.Notifier.Notify();
  }
}
