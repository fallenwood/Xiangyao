namespace Xiangyao.Acme.UnitTests;

using Xiangyao.Acme;

public class AcmeOptionsProviderTests {
  [Fact]
  public void DefaultValues_ShouldBeSet() {
    // Arrange & Act
    var provider = new AcmeOptionsProvider();

    // Assert
    provider.EmailAddress.Should().BeEmpty();
    provider.DomainNames.Should().BeEmpty();
    provider.AcceptTermsOfService.Should().BeTrue();
    provider.ChallengeType.Should().Be(ChallengeType.Http01);
    provider.AcmeDirectoryUrl.Should().Be("https://acme-v02.api.letsencrypt.org/directory");
    provider.CertificateDirectory.Should().Be("./certificates");
  }

  [Fact]
  public void SetDomainNames_ShouldStoreDomains() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    var domains = new[] { "example.com", "www.example.com" };

    // Act
    provider.SetDomainNames(domains);

    // Assert
    provider.DomainNames.Should().HaveCount(2);
    provider.DomainNames.Should().Contain("example.com");
    provider.DomainNames.Should().Contain("www.example.com");
  }

  [Fact]
  public void SetDomainNames_ShouldRemoveDuplicates() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    var domains = new[] { "example.com", "example.com", "www.example.com" };

    // Act
    provider.SetDomainNames(domains);

    // Assert
    provider.DomainNames.Should().HaveCount(2);
  }

  [Fact]
  public void SetDomainNames_ShouldReplaceExistingDomains() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    provider.SetDomainNames(new[] { "old.com" });

    // Act
    provider.SetDomainNames(new[] { "new.com", "other.com" });

    // Assert
    provider.DomainNames.Should().HaveCount(2);
    provider.DomainNames.Should().NotContain("old.com");
    provider.DomainNames.Should().Contain("new.com");
  }

  [Fact]
  public void EmailAddress_ShouldBeSettable() {
    // Arrange
    var provider = new AcmeOptionsProvider();

    // Act
    provider.EmailAddress = "admin@example.com";

    // Assert
    provider.EmailAddress.Should().Be("admin@example.com");
  }

  [Theory]
  [InlineData(ChallengeType.Http01)]
  [InlineData(ChallengeType.Dns01)]
  [InlineData(ChallengeType.TlsAlpn01)]
  public void ChallengeType_ShouldBeSettable(ChallengeType challengeType) {
    // Arrange
    var provider = new AcmeOptionsProvider();

    // Act
    provider.ChallengeType = challengeType;

    // Assert
    provider.ChallengeType.Should().Be(challengeType);
  }

  [Fact]
  public void AcmeDirectoryUrl_ShouldBeSettable() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    var stagingUrl = "https://acme-staging-v02.api.letsencrypt.org/directory";

    // Act
    provider.AcmeDirectoryUrl = stagingUrl;

    // Assert
    provider.AcmeDirectoryUrl.Should().Be(stagingUrl);
  }

  [Fact]
  public void CertificateDirectory_ShouldBeSettable() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    var customPath = "/var/certs";

    // Act
    provider.CertificateDirectory = customPath;

    // Assert
    provider.CertificateDirectory.Should().Be(customPath);
  }

  [Fact]
  public void SetDomainNames_ShouldHandleEmptyCollection() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    provider.SetDomainNames(new[] { "example.com" });

    // Act
    provider.SetDomainNames(Array.Empty<string>());

    // Assert
    provider.DomainNames.Should().BeEmpty();
  }

  [Fact]
  public void DomainNames_ShouldBeReadOnly() {
    // Arrange
    var provider = new AcmeOptionsProvider();
    provider.SetDomainNames(new[] { "example.com" });

    // Assert
    provider.DomainNames.Should().BeAssignableTo<IReadOnlyList<string>>();
  }
}
