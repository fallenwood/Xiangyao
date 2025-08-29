namespace Xiangyao;

using Yarp.ReverseProxy.Configuration;

public interface IXiangyaoProxyConfigProvider : IProxyConfigProvider, IUpdateConfig {

  public string Provider { get; }

  /// <summary>
  /// Different from <see cref="IProxyConfigProvider.GetConfig"/>,
  /// it will not update the exisitng config with <see cref="Config"/>
  /// </summary>
  public XiangyaoProxyConfig Config { get; }

  public IChangeNotifier Notifier { get; }
}
