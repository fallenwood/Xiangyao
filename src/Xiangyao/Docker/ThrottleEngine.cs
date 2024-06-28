namespace Xiangyao;

public interface IThroutteEngine {
  public ValueTask<bool> ThrottleAsync();
}

public class ThroutteEngine(TimeSpan window, int limit) : IThroutteEngine {
  public byte throuttled = 0;
  public int count = 0;

  public async ValueTask<bool> ThrottleAsync() {
    if (this.throuttled == 1) {
      return true;
    }

    var count = Interlocked.Increment(ref this.count);

    if (count == limit) {
      this.throuttled = 1;
      await Task.Delay(window);
      this.count = 0;
      this.throuttled = 0;
      return false;
    }

    if (count > limit) {
      return true;
    }

    return false;
  }
}
