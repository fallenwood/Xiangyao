namespace Xiangyao;

using Yarp.ReverseProxy.Configuration;

public class FileProxyConfigProvider(IProxyConfigProvider configProvider) : IXiangyaoProxyConfigProvider {
  public XiangyaoProxyConfig Config => new (configProvider.GetConfig());

  public IProxyConfig GetConfig() {
    return configProvider.GetConfig();
  }

  public ValueTask UpdateAsync() {
    return ValueTask.CompletedTask;
  }
}