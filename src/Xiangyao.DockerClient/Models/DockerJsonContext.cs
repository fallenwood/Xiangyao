namespace Xiangyao.Docker;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(MonitorEvent))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ListContainerDockerResponse[]))]
public partial class DockerJsonContext : JsonSerializerContext {

}
