namespace Xiangyao;

public interface DockerSocket {
public record DockerUnixDomainSocket(string SocketPath) : DockerSocket;
public record DockerHttpSocket(string Url): DockerSocket;
}
