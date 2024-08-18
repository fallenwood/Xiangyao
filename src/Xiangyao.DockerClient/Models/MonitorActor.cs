namespace Xiangyao.Docker;

using System.Text.Json.Serialization;

public record MonitorActor {
  [JsonPropertyName("ID")]
  public string ID { get; set; } = string.Empty;
  [JsonPropertyName("Attributes")]
  public Dictionary<string, string> Attributes = new();
}
