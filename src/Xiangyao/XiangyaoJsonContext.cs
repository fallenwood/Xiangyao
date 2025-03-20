namespace Xiangyao;

using System.Text.Json.Serialization;


[JsonSerializable(typeof(XiangyaoProxyConfig))]
public partial class XiangyaoJsonContext : JsonSerializerContext {
}
