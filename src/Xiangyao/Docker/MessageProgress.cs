namespace Xiangyao;

using Xiangyao.Docker;

public class MessageProgress(IUpdateConfig updateConfig, ILogger logger) : IProgress<MonitorEvent> {
  private readonly IUpdateConfig updateConfig = updateConfig;
  private readonly ILogger logger = logger;

  public void Report(MonitorEvent message) {
    logger.LogDebug("New Message {Id} {Action}", message.Id, message.Action);

    if (message.Action == "start"
      || message.Action == "die"
      || message.Action.StartsWith("health_status")) {
      updateConfig.Update();
    }
  }
}
