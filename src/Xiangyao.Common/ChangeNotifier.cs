namespace Xiangyao;

using System.Threading;

public interface IChangeNotifier {
  public CancellationTokenSource Source { get; }
  public void Notify();
  public int ResetCount();
  public ValueTask HandleAsync();
}

public class NoopChangeNotifier : IChangeNotifier {
  public CancellationTokenSource Source { get; } = new();
  public ValueTask HandleAsync() {
    return ValueTask.CompletedTask;
  }

  public void Notify() { }

  public int ResetCount() => 0;
}

public class ChangeNotifier(Func<ValueTask> action) : IChangeNotifier {
  private CancellationTokenSource source = new();
  private int count = 0;

  public CancellationTokenSource Source => this.source;

  public CancellationTokenSource ResetCancellationTokenSource() => Interlocked.Exchange(ref this.source, new CancellationTokenSource());

  public void Notify() {
    var source = Interlocked.Exchange(ref this.source, new CancellationTokenSource());
    Interlocked.Increment(ref this.count);
    source.Cancel();
  }

  public int ResetCount() => Interlocked.Exchange(ref this.count, 0);

  public async ValueTask HandleAsync() {
    await action();
  }
}
