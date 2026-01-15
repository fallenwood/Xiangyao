namespace Xiangyao.Acme.UnitTests;

using Xiangyao.Acme;

public class Dns01ChallengeStoreTests {
  [Fact]
  public void AddChallenge_ShouldStoreTxtRecord() {
    // Arrange
    var store = new Dns01ChallengeStore();
    var domain = "example.com";
    var txtRecord = "base64url-encoded-hash";

    // Act
    store.AddChallenge(domain, txtRecord);
    var result = store.GetTxtRecord(domain);

    // Assert
    result.Should().Be(txtRecord);
  }

  [Fact]
  public void GetTxtRecord_ShouldReturnNull_WhenDomainNotFound() {
    // Arrange
    var store = new Dns01ChallengeStore();

    // Act
    var result = store.GetTxtRecord("nonexistent.com");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void RemoveChallenge_ShouldRemoveStoredChallenge() {
    // Arrange
    var store = new Dns01ChallengeStore();
    var domain = "example.com";
    var txtRecord = "base64url-encoded-hash";
    store.AddChallenge(domain, txtRecord);

    // Act
    store.RemoveChallenge(domain);
    var result = store.GetTxtRecord(domain);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void AddChallenge_ShouldOverwriteExistingChallenge() {
    // Arrange
    var store = new Dns01ChallengeStore();
    var domain = "example.com";
    var oldTxtRecord = "old-record";
    var newTxtRecord = "new-record";

    // Act
    store.AddChallenge(domain, oldTxtRecord);
    store.AddChallenge(domain, newTxtRecord);
    var result = store.GetTxtRecord(domain);

    // Assert
    result.Should().Be(newTxtRecord);
  }

  [Theory]
  [InlineData("example.com")]
  [InlineData("sub.example.com")]
  [InlineData("deep.sub.example.com")]
  public void Store_ShouldHandleVariousDomainFormats(string domain) {
    // Arrange
    var store = new Dns01ChallengeStore();
    var txtRecord = "test-record";

    // Act
    store.AddChallenge(domain, txtRecord);
    var result = store.GetTxtRecord(domain);

    // Assert
    result.Should().Be(txtRecord);
  }

  [Fact]
  public void Store_ShouldHandleMultipleDomains() {
    // Arrange
    var store = new Dns01ChallengeStore();
    var challenges = new Dictionary<string, string> {
      { "example.com", "record1" },
      { "test.com", "record2" },
      { "sub.example.com", "record3" }
    };

    // Act
    foreach (var (domain, txtRecord) in challenges) {
      store.AddChallenge(domain, txtRecord);
    }

    // Assert
    foreach (var (domain, txtRecord) in challenges) {
      store.GetTxtRecord(domain).Should().Be(txtRecord);
    }
  }

  [Fact]
  public void RemoveChallenge_ShouldNotThrow_WhenDomainNotFound() {
    // Arrange
    var store = new Dns01ChallengeStore();

    // Act & Assert
    var action = () => store.RemoveChallenge("nonexistent.com");
    action.Should().NotThrow();
  }
}
