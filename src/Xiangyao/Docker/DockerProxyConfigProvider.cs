namespace Xiangyao;

using System.Threading;
using Xiangyao.Docker;
using Xiangyao.Telemetry;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using YRC = Yarp.ReverseProxy.Configuration;

internal sealed class DockerProxyConfigProvider : IXiangyaoProxyConfigProvider {
  public const string UnixSocket = "UnixSocket";

  private readonly IDockerProvider dockerProvider;
  private readonly ILogger<DockerProxyConfigProvider> logger;
  private readonly ILabelParser labelParser;
  private readonly IAcmeDomainProvider acmeDomainProvider;
  private readonly OpenTelemetryMeterProvider? meterProvider;
  private readonly SemaphoreSlim refreshLock = new(1, 1);

  private CancellationTokenSource source = new();
  private volatile bool hasLoadedConfig;

  private XiangyaoProxyConfig config;

  public DockerProxyConfigProvider(
        IDockerProvider dockerProvider,
        ILabelParser labelParser,
        IAcmeDomainProvider acmeDomainProvider,
        ILogger<DockerProxyConfigProvider> logger,
        IServiceProvider serviceProvider) {
    this.dockerProvider = dockerProvider;
    this.logger = logger;
    this.labelParser = labelParser;
    this.acmeDomainProvider = acmeDomainProvider;
    this.meterProvider = serviceProvider.GetService<OpenTelemetryMeterProvider>();
    this.Notifier = new ChangeNotifier(this.UpdateImplAsync);

    this.config = new(
      ProxyConfig: new DockerProxyConfig(
      [],
      [],
      this.source.Token));
  }

  public XiangyaoProxyConfig Config => this.config;

  public IChangeNotifier Notifier { get; private set; }

  public IProxyConfig GetConfig() {
    this.logger.LogDebug(nameof(GetConfig));

    if (!this.hasLoadedConfig) {
      this.RefreshConfigAsync(forceRefresh: false).GetAwaiter().GetResult();
    }

    var currentConfig = this.config.ProxyConfig;

    this.logger.LogInformation("Current Config: {Routes} Routes, {Clusters} Clusters", currentConfig.Routes.Count, currentConfig.Clusters.Count);

    return currentConfig;
  }

  public void Update() {
    this.logger.LogDebug(nameof(Update));

    this.Notifier.Notify();
  }

  private async Task<XiangyaoProxyConfig> BuildXiangyaoProxyConfigAsync(CancellationToken changeToken) {
    this.logger.LogDebug(nameof(BuildXiangyaoProxyConfigAsync));

    var client = this.dockerProvider.DockerClient;
    var allContainers = await client.ListContainersAsync();

    if (this.logger.IsEnabled(LogLevel.Debug)) {
      foreach (var container in allContainers) {
        this.logger.LogDebug("Container {Id} {Name} {Status}", container.Id, GetContainerName(container), container.Status);
      }
    }

    var routes = new List<YRC.RouteConfig>(allContainers.Length);
    var clusters = new List<YRC.ClusterConfig>(allContainers.Length);

    foreach (var container in allContainers) {
      var containerLabels = this.ParseContainerLabels(container.Labels);

      if (!containerLabels.Enabled) {
        this.logger.LogInformation("Container {ContainerId} is not enabled", container.Id);
        this.meterProvider?.RecordDockerMiss();
        continue;
      }

      this.meterProvider?.RecordDockerHit();

      var clusterId = GetContainerName(container);
      var cluster = this.CreateClusterConfig(container, clusterId, containerLabels);

      if (cluster is null) {
        continue;
      }

      clusters.Add(cluster);

      foreach (var route in CreateRouteConfigs(clusterId, containerLabels.Routes)) {
        this.logger.LogDebug("Adding routing for {RouteId} -> {ClusterId}", route.RouteId, route.ClusterId);
        routes.Add(route);
      }
    }

    return new(new DockerProxyConfig(routes, clusters, changeToken));
  }

  private YRC.ClusterConfig? CreateClusterConfig(
    ListContainerResponse container,
    string clusterId,
    ContainerLabels containerLabels) {
    if (containerLabels.Schema == "http" || containerLabels.Schema == "https") {
      var host = containerLabels.CustomHost switch {
        null or "" => this.labelParser.ParseHost(container),
        _ => containerLabels.CustomHost,
      };

      if (string.IsNullOrEmpty(host)) {
        this.logger.LogInformation("No valid host found for {Name}", clusterId);
        return null;
      }

      var address = $"{containerLabels.Schema}://{host}:{containerLabels.Port}";

      return new YRC.ClusterConfig {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, YRC.DestinationConfig>(1) {
          {
            clusterId,
            new() {
              Address = address,
            }
          }
        },
      };
    }

    if (containerLabels.Schema == "unix") {
      var host = containerLabels.CustomHost ?? "localhost";
      var address = $"http://{host}";

      if (this.logger.IsEnabled(LogLevel.Debug)) {
        this.logger.LogDebug("Adding Unix Socket {SocketPath} for {ClusterId} with {Address}", containerLabels.SocketPath, clusterId, address);
      }

      return new YRC.ClusterConfig {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, YRC.DestinationConfig>(1) {
          {
            clusterId,
            new() {
              Address = address,
            }
          },
        },
        Metadata = new Dictionary<string, string>(1) {
          { UnixSocket, containerLabels.SocketPath },
        },
      };
    }

    this.logger.LogWarning("Unknown schema {Schema}", containerLabels.Schema);
    return null;
  }

  private ContainerLabels ParseContainerLabels(Label[] labels) {
    var routes = new DefaultDictionary<string, RouteConfig>(capacity: labels.Length);
    var enabled = false;
    var schema = XiangyaoConstants.Http;
    var port = XiangyaoConstants.HttpPort;
    string? customHost = null;
    var socketPath = string.Empty;

    foreach (var label in labels) {
      if (!label.Name.StartsWith(XiangyaoConstants.LabelKeyPrefix, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      if (string.Equals(label.Name, XiangyaoConstants.EnableLabelKey, StringComparison.OrdinalIgnoreCase)) {
        enabled = string.Equals(label.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
        continue;
      }

      if (string.Equals(label.Name, XiangyaoConstants.SchemaLabelKey, StringComparison.OrdinalIgnoreCase)) {
        schema = string.IsNullOrEmpty(label.Value)
          ? XiangyaoConstants.Http
          : label.Value;
        continue;
      }

      if (string.Equals(label.Name, XiangyaoConstants.HostLabelKey, StringComparison.OrdinalIgnoreCase)) {
        customHost = label.Value;
        continue;
      }

      if (string.Equals(label.Name, XiangyaoConstants.PortLabelKey, StringComparison.OrdinalIgnoreCase)) {
        port = int.TryParse(label.Value, out var parsedPort)
          ? parsedPort
          : XiangyaoConstants.HttpPort;
        continue;
      }

      if (string.Equals(label.Name, XiangyaoConstants.UnixSocketPathLabelKey, StringComparison.OrdinalIgnoreCase)) {
        socketPath = label.Value ?? string.Empty;
        continue;
      }

      if (!label.Name.StartsWith(XiangyaoConstants.RoutesLabelKeyPrefix, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      _ = this.labelParser.Parse(label, routes);
    }

    return new(
      Enabled: enabled,
      Schema: schema,
      Port: port,
      CustomHost: customHost,
      SocketPath: socketPath,
      Routes: routes.ToDictionary());
  }

  private static List<YRC.RouteConfig> CreateRouteConfigs(
    string clusterId,
    IReadOnlyDictionary<string, RouteConfig> parsedLabels) {
    var routes = new List<YRC.RouteConfig>(parsedLabels.Count);

    foreach (var routeEntry in parsedLabels) {
      var route = routeEntry.Value;
      var config = new YRC.RouteConfig {
        RouteId = routeEntry.Key,
        Match = new YRC.RouteMatch {
          Hosts = route.Match.Hosts,
          Path = route.Match.Path,
        },
        ClusterId = clusterId,
      };

      routes.Add(config.WithTransformUseOriginalHostHeader(useOriginal: true));
    }

    return routes;
  }

  private static string GetContainerName(ListContainerResponse container) {
    if (container.Names.Length > 0) {
      return container.Names[0];
    }

    return container.Id;
  }

  private void UpdateAcmeDomains(XiangyaoProxyConfig newConfig) {
    var addressSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var route in newConfig.ProxyConfig.Routes) {
      if (route.Match.Hosts is null) {
        continue;
      }

      foreach (var host in route.Match.Hosts) {
        addressSet.Add(host);
      }
    }

    if (addressSet.Count > 0) {
      var addresses = addressSet.ToArray();
      this.acmeDomainProvider.SetDomainNames(addresses);

      if (this.logger.IsEnabled(LogLevel.Debug)) {
        this.logger.LogDebug("New Addresses {hosts}", string.Join(",", addresses));
        this.logger.LogDebug("New Configuration {Configuration}", System.Text.Json.JsonSerializer.Serialize(this.config));
      }
    } else {
      this.logger.LogInformation("No addresses found in new configuration");
    }
  }

  private async ValueTask RefreshConfigAsync(bool forceRefresh = true) {
    await this.refreshLock.WaitAsync();

    try {
      if (!forceRefresh && this.hasLoadedConfig) {
        return;
      }

      var nextSource = new CancellationTokenSource();
      XiangyaoProxyConfig nextConfig;

      try {
        nextConfig = await this.BuildXiangyaoProxyConfigAsync(nextSource.Token);
      } catch {
        nextSource.Dispose();
        throw;
      }

      Interlocked.Exchange(ref this.config, nextConfig);
      var previousSource = Interlocked.Exchange(ref this.source, nextSource);
      this.hasLoadedConfig = true;

      this.UpdateAcmeDomains(nextConfig);
      previousSource.Cancel();
    }
    finally {
      this.refreshLock.Release();
    }
  }

  private async ValueTask UpdateImplAsync() {
    await this.RefreshConfigAsync();
  }

  private readonly record struct ContainerLabels(
    bool Enabled,
    string Schema,
    int Port,
    string? CustomHost,
    string SocketPath,
    IReadOnlyDictionary<string, RouteConfig> Routes);
}
