namespace Xiangyao;

public class DefaultDictionary<TKey, TValue>(int capacity = 0)
    where TKey : notnull
    where TValue : new() {
  private readonly Dictionary<TKey, TValue> dictionary = new(capacity: capacity);

  public TValue? this[TKey key] {
    get {
      if (this.TryGetValue(key, out var value)) {
        return value;
      }

      value = new TValue();
      dictionary.Add(key, value);

      return value;
    }
  }

  public ICollection<TValue> Values => dictionary.Values;

  public bool TryGetValue(TKey key, out TValue? value) {
    return dictionary.TryGetValue(key, out value);
  }

  public IReadOnlyDictionary<TKey, TValue> ToDictionary() => dictionary;
}
