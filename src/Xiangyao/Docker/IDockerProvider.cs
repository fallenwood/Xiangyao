namespace Xiangyao;

using Xiangyao.Docker;

public interface IDockerProvider {
  public IDockerClient DockerClient {get;}
}
