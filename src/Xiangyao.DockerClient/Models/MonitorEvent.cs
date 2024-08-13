namespace Xiangyao.Docker;

public record MonitorEvent {
  public string Id { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public string From { get; set; } = string.Empty;
  public MonitorEventType Type { get; set; } = MonitorEventType.Container;
  public string Action { get; set; } = string.Empty;
  public MonitorActor Actor { get; set; } = new();
  public MonitorScope Scope { get; set; } = MonitorScope.Local;
  public long Time { get; set; } = 0;
  public long TimeNano { get; set; } = 0;
}
