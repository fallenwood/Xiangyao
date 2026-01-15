namespace Xiangyao.Acme.UnitTests;

using Xiangyao.Acme;

public class Http01ChallengeStoreTests {
  [Fact]
  public void AddChallenge_ShouldStoreKeyAuthorization() {
    // Arrange
    var store = new Http01ChallengeStore();
    var token = "test-token-123";
    var keyAuth = "test-token-123.thumbprint";

    // Act
    store.AddChallenge(token, keyAuth);
    var result = store.GetKeyAuthorization(token);

    // Assert
    result.Should().Be(keyAuth);
  }

  [Fact]
  public void GetKeyAuthorization_ShouldReturnNull_WhenTokenNotFound() {
    // Arrange
    var store = new Http01ChallengeStore();

    // Act
    var result = store.GetKeyAuthorization("nonexistent-token");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void RemoveChallenge_ShouldRemoveStoredChallenge() {
    // Arrange
    var store = new Http01ChallengeStore();
    var token = "test-token-123";
    var keyAuth = "test-token-123.thumbprint";
    store.AddChallenge(token, keyAuth);

    // Act
    store.RemoveChallenge(token);
    var result = store.GetKeyAuthorization(token);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void AddChallenge_ShouldOverwriteExistingChallenge() {
    // Arrange
    var store = new Http01ChallengeStore();
    var token = "test-token-123";
    var oldKeyAuth = "old-key-auth";
    var newKeyAuth = "new-key-auth";

    // Act
    store.AddChallenge(token, oldKeyAuth);
    store.AddChallenge(token, newKeyAuth);
    var result = store.GetKeyAuthorization(token);

    // Assert
    result.Should().Be(newKeyAuth);
  }

  [Fact]
  public void Store_ShouldHandleMultipleChallenges() {
    // Arrange
    var store = new Http01ChallengeStore();
    var challenges = new Dictionary<string, string> {
      { "token1", "keyAuth1" },
      { "token2", "keyAuth2" },
      { "token3", "keyAuth3" }
    };

    // Act
    foreach (var (token, keyAuth) in challenges) {
      store.AddChallenge(token, keyAuth);
    }

    // Assert
    foreach (var (token, keyAuth) in challenges) {
      store.GetKeyAuthorization(token).Should().Be(keyAuth);
    }
  }

  [Fact]
  public void RemoveChallenge_ShouldNotThrow_WhenTokenNotFound() {
    // Arrange
    var store = new Http01ChallengeStore();

    // Act & Assert
    var action = () => store.RemoveChallenge("nonexistent-token");
    action.Should().NotThrow();
  }
}
