namespace Xiangyao.Acme.UnitTests;

using Microsoft.Extensions.Configuration;
using Xiangyao.Acme;

public class AcmeConfigurationProviderTests {
  [Fact]
  public void ConfigurationProvider_ShouldLoadOptions() {
    // Arrange
    var optionsProvider = new AcmeOptionsProvider {
      EmailAddress = "admin@example.com",
      AcceptTermsOfService = true,
      ChallengeType = ChallengeType.Http01,
      AcmeDirectoryUrl = "https://acme-staging-v02.api.letsencrypt.org/directory",
      CertificateDirectory = "/var/certs"
    };
    optionsProvider.SetDomainNames(new[] { "example.com", "www.example.com" });

    var configBuilder = new ConfigurationBuilder();
    configBuilder.AddAcmeOptionsProvider(optionsProvider);

    // Act
    var config = configBuilder.Build();

    // Assert
    config["Acme:EmailAddress"].Should().Be("admin@example.com");
    config["Acme:AcceptTermsOfService"].Should().Be("True");
    config["Acme:ChallengeType"].Should().Be("Http01");
    config["Acme:AcmeDirectoryUrl"].Should().Be("https://acme-staging-v02.api.letsencrypt.org/directory");
    config["Acme:CertificateDirectory"].Should().Be("/var/certs");
  }

  [Fact]
  public void ConfigurationProvider_ShouldLoadDomainNames() {
    // Arrange
    var optionsProvider = new AcmeOptionsProvider();
    optionsProvider.SetDomainNames(new[] { "example.com", "www.example.com", "api.example.com" });

    var configBuilder = new ConfigurationBuilder();
    configBuilder.AddAcmeOptionsProvider(optionsProvider);

    // Act
    var config = configBuilder.Build();

    // Assert
    config["Acme:DomainNames:0"].Should().Be("example.com");
    config["Acme:DomainNames:1"].Should().Be("www.example.com");
    config["Acme:DomainNames:2"].Should().Be("api.example.com");
  }

  [Theory]
  [InlineData(ChallengeType.Http01, "Http01")]
  [InlineData(ChallengeType.Dns01, "Dns01")]
  [InlineData(ChallengeType.TlsAlpn01, "TlsAlpn01")]
  public void ConfigurationProvider_ShouldSerializeChallengeType(
    ChallengeType challengeType,
    string expectedValue) {
    // Arrange
    var optionsProvider = new AcmeOptionsProvider {
      ChallengeType = challengeType
    };

    var configBuilder = new ConfigurationBuilder();
    configBuilder.AddAcmeOptionsProvider(optionsProvider);

    // Act
    var config = configBuilder.Build();

    // Assert
    config["Acme:ChallengeType"].Should().Be(expectedValue);
  }

  [Fact]
  public void ConfigurationProvider_ShouldHandleEmptyDomainNames() {
    // Arrange
    var optionsProvider = new AcmeOptionsProvider();

    var configBuilder = new ConfigurationBuilder();
    configBuilder.AddAcmeOptionsProvider(optionsProvider);

    // Act
    var config = configBuilder.Build();

    // Assert
    config["Acme:DomainNames:0"].Should().BeNull();
  }

  [Fact]
  public void AcmeOptionsConfigurationSource_ShouldBuildProvider() {
    // Arrange
    var optionsProvider = new AcmeOptionsProvider();
    var source = new AcmeOptionsConfigurationSource(optionsProvider);
    var configBuilder = new ConfigurationBuilder();

    // Act
    var provider = source.Build(configBuilder);

    // Assert
    provider.Should().NotBeNull();
    provider.Should().BeOfType<AcmeOptionsConfigurationProvider>();
  }
}
