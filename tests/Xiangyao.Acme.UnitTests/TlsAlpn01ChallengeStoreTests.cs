namespace Xiangyao.Acme.UnitTests;

using Xiangyao.Acme;

public class TlsAlpn01ChallengeStoreTests {
  [Fact]
  public void AddChallenge_ShouldGenerateCertificate() {
    // Arrange
    var store = new TlsAlpn01ChallengeStore();
    var domain = "example.com";
    var keyAuthHash = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                   0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
                                   0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                                   0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20 };

    // Act
    store.AddChallenge(domain, keyAuthHash);
    var certificate = store.GetCertificate(domain);

    // Assert
    certificate.Should().NotBeNull();
    certificate!.Subject.Should().Contain(domain);
  }

  [Fact]
  public void GetCertificate_ShouldReturnNull_WhenDomainNotFound() {
    // Arrange
    var store = new TlsAlpn01ChallengeStore();

    // Act
    var result = store.GetCertificate("nonexistent.com");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void RemoveChallenge_ShouldRemoveStoredCertificate() {
    // Arrange
    var store = new TlsAlpn01ChallengeStore();
    var domain = "example.com";
    var keyAuthHash = new byte[32];

    store.AddChallenge(domain, keyAuthHash);

    // Act
    store.RemoveChallenge(domain);
    var result = store.GetCertificate(domain);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void GeneratedCertificate_ShouldHaveValidDates() {
    // Arrange
    var store = new TlsAlpn01ChallengeStore();
    var domain = "example.com";
    var keyAuthHash = new byte[32];

    // Act
    store.AddChallenge(domain, keyAuthHash);
    var certificate = store.GetCertificate(domain);

    // Assert
    certificate.Should().NotBeNull();
    certificate!.NotBefore.Should().BeBefore(DateTime.UtcNow);
    certificate.NotAfter.Should().BeAfter(DateTime.UtcNow);
  }

  [Fact]
  public void Store_ShouldHandleMultipleDomains() {
    // Arrange
    var store = new TlsAlpn01ChallengeStore();
    var domains = new[] { "example1.com", "example2.com", "example3.com" };

    // Act
    foreach (var domain in domains) {
      store.AddChallenge(domain, new byte[32]);
    }

    // Assert
    foreach (var domain in domains) {
      var cert = store.GetCertificate(domain);
      cert.Should().NotBeNull();
      cert!.Subject.Should().Contain(domain);
    }
  }

  [Fact]
  public void RemoveChallenge_ShouldNotThrow_WhenDomainNotFound() {
    // Arrange
    var store = new TlsAlpn01ChallengeStore();

    // Act & Assert
    var action = () => store.RemoveChallenge("nonexistent.com");
    action.Should().NotThrow();
  }
}
