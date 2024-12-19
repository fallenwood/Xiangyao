namespace Xiangyao;

public static class XiangyaoConstants {
  public const string Xiangyao = "xiangyao";

  public const string LabelKeyPrefix = $"{Xiangyao}";

  public const string EnableLabelKey = $"{LabelKeyPrefix}.enable";

  public const string SchemaLabelKey = $"{LabelKeyPrefix}.cluster.schema";

  public const string PortLabelKey = $"{LabelKeyPrefix}.cluster.port";

  public const string UnixSocketPathLabelKey = $"{LabelKeyPrefix}.cluster.socketpath";

  public const string HostLabelKey = $"{LabelKeyPrefix}.cluster.host";

  public const string Http = "http";

  public const int HttpPort = 80;

  public const string Label = "label";
}
