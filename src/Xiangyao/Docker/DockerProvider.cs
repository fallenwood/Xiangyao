namespace Xiangyao;

using Docker.DotNet;

internal sealed class DockerProvider : IDockerProvider {
  public DockerClient CreateDockerClient() {
    var client = new DockerClientConfiguration()
        .CreateClient();

    return client;
  }
}
