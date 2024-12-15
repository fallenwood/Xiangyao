namespace Xiangyao;

using Xiangyao.Docker;

internal class DockerMonitorHostedService(
    IDockerProvider dockerProvider,
    IXiangyaoProxyConfigProvider proxyConfigProvider,
    ILogger<DockerMonitorHostedService> logger,
    ILoggerFactory loggerFactory)
    : BackgroundService {
  private const int MaxStep = 120;
  private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

  private int step = 1;

  private readonly Dictionary<string, IDictionary<string, bool>> Filter = new(1) {
    {
      XiangyaoConstants.Label,
      new Dictionary<string, bool>(1) {
        { XiangyaoConstants.EnableLabelKey, true },
      }
    },
  };

  private IDockerClient dockerClient = default!;

  private Task backgroundTask = default!;

  public override async Task StartAsync(CancellationToken cancellationToken) {
    logger.LogInformation("Starting...");

    this.dockerClient = dockerProvider.DockerClient;

    this.backgroundTask = dockerClient.MonitorEventsAsync(
        new ContainerEventsParameters {
        },
        new MessageProgress(proxyConfigProvider, loggerFactory.CreateLogger<MessageProgress>()),
        cancellationToken: cancellationToken);

    proxyConfigProvider.Update();

    await base.StartAsync(cancellationToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken) {
    logger.LogInformation("Stopping...");
    this.backgroundTask = default!;
    await base.StopAsync(cancellationToken);
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    while (!stoppingToken.IsCancellationRequested) {
      var lastCount = proxyConfigProvider.Notifier.ResetCount();

      try {
        await proxyConfigProvider.Notifier.HandleAsync();
      } catch (Exception ex) {
        logger.LogError(ex, "Error updating configuration");
      }

      if (lastCount > 0) {
        step = 1;
      } else {
        step = Math.Min(step * 2, MaxStep);
      }

      await Task.Delay(step * Interval, stoppingToken);
    }
  }
}
