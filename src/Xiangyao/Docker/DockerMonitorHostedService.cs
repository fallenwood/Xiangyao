namespace Xiangyao;

using Xiangyao.Docker;

internal class DockerMonitorHostedService(
    IDockerProvider dockerProvider,
    IUpdateConfig updateConfig,
    ILogger<DockerMonitorHostedService> logger,
    ILoggerFactory loggerFactory)
    : IHostedService {

  private readonly Dictionary<string, IDictionary<string, bool>> Filter = new(1) {
    {
      XiangyaoConstants.Label,
      new Dictionary<string, bool>(1) {
        { XiangyaoConstants.EnableLabelKey, true },
      }
    },
  };

  private readonly IDockerProvider dockerProvider = dockerProvider;
  private readonly IUpdateConfig updateConfig = updateConfig;
  private readonly ILogger<DockerMonitorHostedService> logger = logger;
  private IDockerClient dockerClient = default!;

  private Task backgroundTask = default!;

  public async Task StartAsync(CancellationToken cancellationToken) {
    logger.LogInformation("Starting...");

    this.dockerClient = this.dockerProvider.DockerClient;

    this.backgroundTask = dockerClient.MonitorEventsAsync(
        new ContainerEventsParameters {
        },
        new MessageProgress(updateConfig, loggerFactory.CreateLogger<MessageProgress>()),
        cancellationToken: cancellationToken);

    await updateConfig.UpdateAsync();
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    logger.LogInformation("Stopping...");
    this.backgroundTask = default!;
    return Task.CompletedTask;
  }
}
