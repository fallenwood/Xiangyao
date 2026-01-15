namespace Xiangyao.Acme.UnitTests;

using Xiangyao.Acme;

public class AcmeClientTests {
  [Fact]
  public void Constructor_ShouldUseDefaultDirectoryUrl() {
    // Act
    using var client = new AcmeClient();

    // Assert - constructor should not throw
    client.Should().NotBeNull();
  }

  [Fact]
  public void Constructor_ShouldAcceptCustomDirectoryUrl() {
    // Arrange
    var stagingUrl = "https://acme-staging-v02.api.letsencrypt.org/directory";

    // Act
    using var client = new AcmeClient(stagingUrl);

    // Assert - constructor should not throw
    client.Should().NotBeNull();
  }

  [Fact]
  public void GetKeyAuthorization_ShouldReturnTokenWithThumbprint() {
    // Arrange
    using var client = new AcmeClient();
    var token = "test-token-abc123";

    // Act
    var keyAuth = client.GetKeyAuthorization(token);

    // Assert
    keyAuth.Should().StartWith(token + ".");
    keyAuth.Length.Should().BeGreaterThan(token.Length + 1);
  }

  [Fact]
  public void GetKeyAuthorization_ShouldBeConsistent() {
    // Arrange
    using var client = new AcmeClient();
    var token = "test-token-abc123";

    // Act
    var keyAuth1 = client.GetKeyAuthorization(token);
    var keyAuth2 = client.GetKeyAuthorization(token);

    // Assert
    keyAuth1.Should().Be(keyAuth2);
  }

  [Fact]
  public void GetDns01TxtRecord_ShouldReturnBase64UrlEncodedHash() {
    // Arrange
    using var client = new AcmeClient();
    var token = "test-token-abc123";

    // Act
    var txtRecord = client.GetDns01TxtRecord(token);

    // Assert
    txtRecord.Should().NotBeNullOrEmpty();
    txtRecord.Should().NotContain("+"); // Base64Url should not contain +
    txtRecord.Should().NotContain("/"); // Base64Url should not contain /
    txtRecord.Should().NotContain("="); // Base64Url should not have padding
  }

  [Fact]
  public void GetDns01TxtRecord_ShouldBeConsistent() {
    // Arrange
    using var client = new AcmeClient();
    var token = "test-token-abc123";

    // Act
    var record1 = client.GetDns01TxtRecord(token);
    var record2 = client.GetDns01TxtRecord(token);

    // Assert
    record1.Should().Be(record2);
  }

  [Fact]
  public void GetTlsAlpn01KeyAuthorizationHash_ShouldReturn32Bytes() {
    // Arrange
    using var client = new AcmeClient();
    var token = "test-token-abc123";

    // Act
    var hash = client.GetTlsAlpn01KeyAuthorizationHash(token);

    // Assert
    hash.Should().HaveCount(32); // SHA-256 produces 32 bytes
  }

  [Fact]
  public void GetTlsAlpn01KeyAuthorizationHash_ShouldBeConsistent() {
    // Arrange
    using var client = new AcmeClient();
    var token = "test-token-abc123";

    // Act
    var hash1 = client.GetTlsAlpn01KeyAuthorizationHash(token);
    var hash2 = client.GetTlsAlpn01KeyAuthorizationHash(token);

    // Assert
    hash1.Should().BeEquivalentTo(hash2);
  }

  [Theory]
  [InlineData("token1")]
  [InlineData("token2")]
  [InlineData("a-very-long-token-with-many-characters-123456789")]
  public void GetKeyAuthorization_ShouldWorkWithDifferentTokens(string token) {
    // Arrange
    using var client = new AcmeClient();

    // Act
    var keyAuth = client.GetKeyAuthorization(token);

    // Assert
    keyAuth.Should().StartWith(token + ".");
  }

  [Fact]
  public void DifferentClients_ShouldProduceDifferentKeyAuthorizations() {
    // Arrange
    using var client1 = new AcmeClient();
    using var client2 = new AcmeClient();
    var token = "test-token";

    // Act
    var keyAuth1 = client1.GetKeyAuthorization(token);
    var keyAuth2 = client2.GetKeyAuthorization(token);

    // Assert - Different clients have different account keys
    keyAuth1.Should().NotBe(keyAuth2);
  }

  [Fact]
  public void Dispose_ShouldNotThrow() {
    // Arrange
    var client = new AcmeClient();

    // Act & Assert
    var action = () => client.Dispose();
    action.Should().NotThrow();
  }
}
