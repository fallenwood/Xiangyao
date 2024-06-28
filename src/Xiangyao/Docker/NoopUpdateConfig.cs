namespace Xiangyao;

public class NoopUpdateConfig : IUpdateConfig {
  public ValueTask UpdateAsync() {
    return ValueTask.CompletedTask;
  }
}
