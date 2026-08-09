using Microsoft.Extensions.Logging.Abstractions;
using Xiangyao.Docker;

namespace Xiangyao.UnitTests.Docker;

public class DockerProxyConfigProviderTests {
  [Fact]
  public void GetConfig_WhenInitialDockerLoadFails_ReturnsEmptyConfigAndSchedulesRetry() {
    var dockerClient = new TestDockerClient();
    var provider = new DockerProxyConfigProvider(
      new TestDockerProvider(dockerClient),
      new SwitchCaseLabelParser(),
      new AcmeDomainProvider(),
      NullLogger<DockerProxyConfigProvider>.Instance,
      new EmptyServiceProvider());

    var config = provider.GetConfig();

    config.Routes.Should().BeEmpty();
    config.Clusters.Should().BeEmpty();
    provider.Notifier.ResetCount().Should().Be(1);
    dockerClient.ListContainersCallCount.Should().Be(1);
  }

  private sealed class TestDockerProvider(IDockerClient dockerClient) : IDockerProvider {
    public IDockerClient DockerClient { get; } = dockerClient;
  }

  private sealed class TestDockerClient : IDockerClient {
    public int ListContainersCallCount { get; private set; }

    public ValueTask<ListContainerResponse[]> ListContainersAsync() {
      this.ListContainersCallCount++;

      if (this.ListContainersCallCount == 1) {
        return ValueTask.FromException<ListContainerResponse[]>(new HttpRequestException("Docker is not ready"));
      }

      return ValueTask.FromResult<ListContainerResponse[]>([]);
    }

    public Task MonitorEventsAsync(
      ContainerEventsParameters parameters,
      IProgress<MonitorEvent> progress,
      CancellationToken cancellationToken) {
      return Task.CompletedTask;
    }
  }

  private sealed class EmptyServiceProvider : IServiceProvider {
    public object? GetService(Type serviceType) {
      return null;
    }
  }
}
