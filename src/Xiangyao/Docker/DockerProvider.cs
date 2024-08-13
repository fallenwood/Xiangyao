namespace Xiangyao;

using Xiangyao.Docker;



internal sealed class DockerProvider : IDockerProvider {
  public IDockerClient CreateDockerClient() {
    var client = new DockerClient(new HttpClient());

    return client;
  }
}
