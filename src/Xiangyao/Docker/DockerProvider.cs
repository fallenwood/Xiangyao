namespace Xiangyao;

using System.Net.Sockets;
using Xiangyao.Docker;

internal sealed class DockerProvider  : IDockerProvider {
  private readonly Lazy<IDockerClient> lazyDockerClient;
  public DockerProvider(DockerSocket dockerSocket) {
    this.lazyDockerClient = new Lazy<IDockerClient>(() => this.CreateDockerClient(dockerSocket));
  }

  public IDockerClient DockerClient => this.lazyDockerClient.Value;

  private IDockerClient CreateDockerClient(DockerSocket dockerSocket) {
    var client = dockerSocket switch {
      DockerSocket.DockerUnixDomainSocket uds => new DockerClient(
        new HttpClient(
          new SocketsHttpHandler {
            ConnectCallback = async (context, token) => {
              var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
              var endpoint = new UnixDomainSocketEndPoint(uds.SocketPath);
              await socket.ConnectAsync(endpoint);
              return new NetworkStream(socket, ownsSocket: true);
            }
          })),

      DockerSocket.DockerHttpSocket hs => new DockerClient(new HttpClient(), hs.Url),
      _ => throw new NotImplementedException(),
    };

    return client;
  }
}
