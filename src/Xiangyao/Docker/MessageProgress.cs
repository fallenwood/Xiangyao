namespace Xiangyao;

using Docker.DotNet.Models;

public class MessageProgress(IUpdateConfig updateConfig, ILogger logger) : IProgress<Message> {
  private readonly IUpdateConfig updateConfig = updateConfig;
  private readonly ILogger logger = logger;

  public void Report(Message message) {
    logger.LogDebug("New Message {Id} {Action}", message.ID, message.Action);

    if (message.Action == "start"
      || message.Action == "die"
      || message.Action.StartsWith("health_status")) {
      _ = updateConfig.UpdateAsync();
    }
  }
}
