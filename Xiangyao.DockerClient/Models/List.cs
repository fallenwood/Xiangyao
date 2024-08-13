using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xiangyao.Docker;

public record ErrorResponse {
  public string Message { get; set; } = string.Empty;
}

public record RegistryAuth {
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string ServerAddress { get; set; } = string.Empty;
  public string IdentityToken { get; set; } = string.Empty;
}

public record ListContainersRequest {
  public bool All { get; set; } = false;
  public int Limit { get; set; } = 0;
  public bool Size { get; set; } = false;
  public string Filters { get; set; } = string.Empty;
}
public record ListContainerResponse {
  public string Id { get; set; } = string.Empty;
  public string[] Names { get; set; } = [];
  public string Image { get; set; } = string.Empty;
  public string ImageID { get; set; } = string.Empty;
  public string Command { get; set; } = string.Empty;
  public long Created { get; set; } = 0;
  public PortType[] Ports { get; set; } = [];
  public long[] SizeRw { get; set; } = [];
  public long[] SizeRootFs { get; set; } = [];
  public Dictionary<string, string> Labels { get; set; } = [];
  public string State { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public HostConfig HostConfig { get; set; } = new();
  public Dictionary<string, NetworkEntry> NetworkSettings { get; set; } = [];
  public object[] Mounts { get; set; } = [];
}

public enum MountType {
  Bind,
  Volume,
  TmpFs,
  NPipe,
  Cluster,
}

public class Mount {
  public MountType Type { get; set; } = MountType.Bind;
  public string Name { get; set; } = string.Empty;
  public string Source { get; set; } = string.Empty;
  public string Destination { get; set; } = string.Empty;
  public string Driver { get; set; } = string.Empty;
  public string Mode { get; set; } = string.Empty;
  public bool Rw { get; set; } = false;
  public string Propagation { get; set; } = string.Empty;
}

public record EndpointSettings {
  public Networks Networks { get; set; } = new();
}

public record Networks {
  public NetworkEntry? Bridge { get; set; }
}

public record NetworkHost {
  public string NetworkID { get; set; } = string.Empty;
  public string EndpointID { get; set; } = string.Empty;
  public string Gateway { get; set; } = string.Empty;
  public string IPAddress { get; set; } = string.Empty;
  public int IPPrefixLen { get; set; } = 0;
  public string IPv6Gateway { get; set; } = string.Empty;
  public string GlobalIPv6Address { get; set; } = string.Empty;
  public int GlobalIPv6PrefixLen { get; set; } = 0;
  public string MacAddress { get; set; } = string.Empty;
}

public record NetworkEntry {
  public NetworkHost Host { get; set; } = new();
}

public enum PortType {
  TCP,
  UDP,
  SCTP,
}

public record Port {
  public string IP { get; set; } = string.Empty;
  public ushort PrivatePort { get; set; } = 0;
  public ushort PublicPort { get; set; } = 0;
  public PortType Type { get; set; } = PortType.TCP;
}

public record HostConfig {
  public string NetworkMode { get; set; } = string.Empty;
  public Dictionary<string, string>? Annotations { get; set; }
}
