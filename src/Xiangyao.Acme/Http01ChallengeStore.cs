namespace Xiangyao.Acme;

using System.Collections.Concurrent;

public interface IHttp01ChallengeStore {
  void AddChallenge(string token, string keyAuthorization);
  string? GetKeyAuthorization(string token);
  void RemoveChallenge(string token);
}

public class Http01ChallengeStore : IHttp01ChallengeStore {
  private readonly ConcurrentDictionary<string, string> _challenges = new();

  public void AddChallenge(string token, string keyAuthorization) {
    _challenges[token] = keyAuthorization;
  }

  public string? GetKeyAuthorization(string token) {
    return _challenges.TryGetValue(token, out var keyAuth) ? keyAuth : null;
  }

  public void RemoveChallenge(string token) {
    _challenges.TryRemove(token, out _);
  }
}
