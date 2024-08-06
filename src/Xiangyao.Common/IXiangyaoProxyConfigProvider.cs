namespace Xiangyao;

using Yarp.ReverseProxy.Configuration;

public interface IXiangyaoProxyConfigProvider : IProxyConfigProvider, IUpdateConfig {
  /// <summary>
  /// Different from <see cref="IProxyConfigProvider.GetConfig"/>, it will not update the exisitng config with <see cref="Config"/>
  /// </summary>
  public XiangyaoProxyConfig Config { get; }
}
