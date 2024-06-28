namespace Xiangyao;

using Docker.DotNet;

public interface IDockerProvider {
  public DockerClient CreateDockerClient();
}
