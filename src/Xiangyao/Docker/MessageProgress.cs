namespace Xiangyao;

using Xiangyao.Docker;

public class MessageProgress(IUpdateConfig updateConfig, ILogger logger) : IProgress<MonitorEvent> {
  private readonly IUpdateConfig updateConfig = updateConfig;
  private readonly ILogger logger = logger;

  public void Report(MonitorEvent message) {
    logger.LogDebug("New Message {Id} {Action}", message.Id, message.Action);

    if (string.Equals(message.Action, "start", StringComparison.Ordinal)
      || string.Equals(message.Action, "die", StringComparison.Ordinal)) {
      updateConfig.Update();
    }
  }
}
