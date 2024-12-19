namespace Xiangyao.Utils;

using System.Net.Sockets;

public static class ReverseProxyBuilderExtensions {

  public static IReverseProxyBuilder ConfigureUnixSocket(this IReverseProxyBuilder builder) {

    builder.ConfigureHttpClient(static (context, handler) => {
      if (context.NewMetadata?.TryGetValue(DockerProxyConfigProvider.UnixSocket, out var unixSocket) == true) {

        handler.ConnectCallback = async (_context, cancellationToken) => {
          var socket = new Socket(
            AddressFamily.Unix,
            SocketType.Stream,
            ProtocolType.IP);

          await socket.ConnectAsync(new UnixDomainSocketEndPoint(unixSocket));

          return new NetworkStream(socket, ownsSocket: true);
        };
      }
    });
    return builder;
  }
}
