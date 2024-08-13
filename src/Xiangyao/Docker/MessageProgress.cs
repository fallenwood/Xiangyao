namespace Xiangyao;

using Xiangyao.Docker;

public class MessageProgress(IUpdateConfig updateConfig, ILogger logger) : IAsyncProgress<MonitorEvent> {
  private readonly IUpdateConfig updateConfig = updateConfig;
  private readonly ILogger logger = logger;

  public ValueTask ReportAsync(MonitorEvent message) {
    logger.LogDebug("New Message {Id} {Action}", message.Id, message.Action);

    if (message.Action == "start"
      || message.Action == "die"
      || message.Action.StartsWith("health_status")) {
      _ =  updateConfig.UpdateAsync();
    }

    return ValueTask.CompletedTask;
  }
}
