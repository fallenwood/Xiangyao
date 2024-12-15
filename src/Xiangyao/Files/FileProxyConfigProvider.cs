namespace Xiangyao;

using Yarp.ReverseProxy.Configuration;

public class FileProxyConfigProvider(IProxyConfigProvider configProvider) : IXiangyaoProxyConfigProvider {
  public XiangyaoProxyConfig Config => new (configProvider.GetConfig());

  public IChangeNotifier Notifier { get; } = new NoopChangeNotifier();

  public IProxyConfig GetConfig() {
    return configProvider.GetConfig();
  }

  public void Update() { }
}
