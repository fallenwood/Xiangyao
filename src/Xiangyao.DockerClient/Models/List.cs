namespace Xiangyao.Docker;

public record ListContainerDockerResponse {
  public string Id { get; set; } = string.Empty;
  public string[] Names { get; set; } = [];
  public Dictionary<string, string> Labels { get; set; } = [];
  public DockerNetworkSettings NetworkSettings { get; set; } = new ();
}


public record ListContainerResponse {
  public string Id { get; set; } = string.Empty;
  public string[] Names { get; set; } = [];
  public Label[] Labels { get; set; } = [];
  public string State { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public NetworkEntry[] NetworkSettings { get; set; } = [];
}

public record DockerNetworkSettings {
  public Dictionary<string, NetworkEntry> Networks { get; set; } = new ();
}

public record DockerNetworks {
  public Dictionary<string, NetworkEntry> Entries = new();
}


public record NetworkEntry {
  public string Name { get; set; } = string.Empty;
  public string IPAddress { get; set; } = string.Empty;
  public string GlobalIPv6Address { get; set; } = string.Empty;
}
