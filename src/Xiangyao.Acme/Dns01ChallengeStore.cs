namespace Xiangyao.Acme;

using System.Collections.Concurrent;

public enum ChallengeType {
  Http01,
  Dns01,
  TlsAlpn01
}

public interface IDns01ChallengeStore {
  void AddChallenge(string domain, string txtRecord);
  string? GetTxtRecord(string domain);
  void RemoveChallenge(string domain);
}

public class Dns01ChallengeStore : IDns01ChallengeStore {
  private readonly ConcurrentDictionary<string, string> _challenges = new();

  public void AddChallenge(string domain, string txtRecord) {
    var recordName = $"_acme-challenge.{domain}";
    _challenges[recordName] = txtRecord;
  }

  public string? GetTxtRecord(string domain) {
    var recordName = $"_acme-challenge.{domain}";
    return _challenges.TryGetValue(recordName, out var record) ? record : null;
  }

  public void RemoveChallenge(string domain) {
    var recordName = $"_acme-challenge.{domain}";
    _challenges.TryRemove(recordName, out _);
  }
}
