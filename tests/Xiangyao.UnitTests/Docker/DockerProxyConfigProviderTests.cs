using Microsoft.Extensions.Logging.Abstractions;
using Xiangyao.Docker;

namespace Xiangyao.UnitTests.Docker;

public class DockerProxyConfigProviderTests {
  [Fact]
  public async Task GetConfig_WhenInitialDockerLoadFails_ReturnsEmptyConfigAndSchedulesRetry() {
    var dockerClient = new TestDockerClient { ShouldFail = true };
    var provider = CreateProvider(dockerClient);

    var config = provider.GetConfig();

    config.Routes.Should().BeEmpty();
    config.Clusters.Should().BeEmpty();

    await provider.InitialLoadTask.WaitAsync(TimeSpan.FromSeconds(2));

    provider.Notifier.ResetCount().Should().Be(1);
    dockerClient.ListContainersCallCount.Should().Be(1);

    var calls = dockerClient.ListContainersCallCount;
    _ = provider.GetConfig();
    await Task.Delay(50);
    dockerClient.ListContainersCallCount.Should().Be(calls);
  }

  [Fact]
  public async Task GetConfig_WhenCalledConcurrently_OnlyStartsOneInitialLoad() {
    var dockerClient = new TestDockerClient {
      ShouldFail = false,
      Delay = TimeSpan.FromMilliseconds(100),
    };
    var provider = CreateProvider(dockerClient);

    var first = provider.GetConfig();
    var second = provider.GetConfig();

    first.Routes.Should().BeEmpty();
    second.Routes.Should().BeEmpty();

    await provider.InitialLoadTask.WaitAsync(TimeSpan.FromSeconds(2));

    dockerClient.ListContainersCallCount.Should().Be(1);

    provider.Notifier.ResetCount();
    _ = provider.GetConfig();
    provider.Notifier.ResetCount().Should().Be(0);
    dockerClient.ListContainersCallCount.Should().Be(1);
  }

  [Fact]
  public async Task GetConfig_WhenInitialDockerLoadSucceeds_CompletesWithoutNotify() {
    var dockerClient = new TestDockerClient { ShouldFail = false };
    var provider = CreateProvider(dockerClient);

    var config = provider.GetConfig();
    config.Routes.Should().BeEmpty();

    await provider.InitialLoadTask.WaitAsync(TimeSpan.FromSeconds(2));

    provider.InitialLoadTask.IsCompletedSuccessfully.Should().BeTrue();
    dockerClient.ListContainersCallCount.Should().Be(1);
    provider.Notifier.ResetCount().Should().Be(0);

    _ = provider.GetConfig();
    await Task.Delay(50);
    dockerClient.ListContainersCallCount.Should().Be(1);
  }

  [Fact]
  public async Task GetConfig_WhenInitialDockerLoadSucceeds_SignalsChangeToken() {
    var dockerClient = new TestDockerClient {
      ShouldFail = false,
      Delay = TimeSpan.FromMilliseconds(50),
    };
    var provider = CreateProvider(dockerClient);

    // Subscribe before scheduling the load so the cancel is observed.
    var initialConfig = provider.Config.ProxyConfig;
    var reload = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var registration = initialConfig.ChangeToken.RegisterChangeCallback(
      static state => ((TaskCompletionSource)state!).TrySetResult(),
      reload);

    _ = provider.GetConfig();

    await provider.InitialLoadTask.WaitAsync(TimeSpan.FromSeconds(2));

    if (initialConfig.ChangeToken.HasChanged) {
      reload.TrySetResult();
    }

    await reload.Task.WaitAsync(TimeSpan.FromSeconds(2));
    dockerClient.ListContainersCallCount.Should().Be(1);
  }

  private static DockerProxyConfigProvider CreateProvider(IDockerClient dockerClient) {
    return new DockerProxyConfigProvider(
      new TestDockerProvider(dockerClient),
      new SwitchCaseLabelParser(),
      new AcmeDomainProvider(),
      NullLogger<DockerProxyConfigProvider>.Instance,
      new EmptyServiceProvider());
  }

  private sealed class TestDockerProvider(IDockerClient dockerClient) : IDockerProvider {
    public IDockerClient DockerClient { get; } = dockerClient;
  }

  private sealed class TestDockerClient : IDockerClient {
    private int listContainersCallCount;

    public int ListContainersCallCount => Volatile.Read(ref this.listContainersCallCount);

    public bool ShouldFail { get; set; } = true;

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public async ValueTask<ListContainerResponse[]> ListContainersAsync() {
      Interlocked.Increment(ref this.listContainersCallCount);

      if (this.Delay > TimeSpan.Zero) {
        await Task.Delay(this.Delay).ConfigureAwait(false);
      }

      if (this.ShouldFail) {
        throw new HttpRequestException("Docker is not ready");
      }

      return Array.Empty<ListContainerResponse>();
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
