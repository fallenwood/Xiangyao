using Microsoft.Extensions.Logging.Abstractions;
using Xiangyao.Docker;

namespace Xiangyao.UnitTests.Docker;

public class MessageProgressTests {
  [Theory]
  [InlineData("start", 1)]
  [InlineData("die", 1)]
  [InlineData("health_status: healthy", 0)]
  [InlineData("health_status: unhealthy", 0)]
  public void Report_OnlyUpdatesConfigForContainerLifecycleEvents(string action, int expectedUpdates) {
    var updateConfig = new TestUpdateConfig();
    var progress = new MessageProgress(updateConfig, NullLogger<MessageProgress>.Instance);

    progress.Report(new MonitorEvent {
      Action = action,
      Id = "container-id",
    });

    updateConfig.UpdateCount.Should().Be(expectedUpdates);
  }

  private sealed class TestUpdateConfig : IUpdateConfig {
    public int UpdateCount { get; private set; }

    public void Update() {
      this.UpdateCount++;
    }
  }
}
