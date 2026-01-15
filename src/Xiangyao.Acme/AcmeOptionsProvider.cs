namespace Xiangyao.Acme;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Interface for providing dynamic ACME options, especially domain names
/// that may change at runtime based on Docker container discovery.
/// </summary>
public interface IAcmeOptionsProvider {
  /// <summary>
  /// Gets or sets the email address for Let's Encrypt registration.
  /// </summary>
  string EmailAddress { get; set; }

  /// <summary>
  /// Gets the domain names to obtain certificates for.
  /// </summary>
  IReadOnlyList<string> DomainNames { get; }

  /// <summary>
  /// Sets the domain names dynamically (e.g., from Docker container discovery).
  /// </summary>
  void SetDomainNames(IEnumerable<string> domainNames);

  /// <summary>
  /// Gets whether Let's Encrypt terms of service are accepted.
  /// </summary>
  bool AcceptTermsOfService { get; set; }

  /// <summary>
  /// Gets the challenge type to use.
  /// </summary>
  ChallengeType ChallengeType { get; set; }

  /// <summary>
  /// Gets the ACME directory URL.
  /// </summary>
  string AcmeDirectoryUrl { get; set; }

  /// <summary>
  /// Gets the certificate storage directory.
  /// </summary>
  string CertificateDirectory { get; set; }
}

/// <summary>
/// Default implementation of IAcmeOptionsProvider.
/// </summary>
public class AcmeOptionsProvider : IAcmeOptionsProvider {
  private List<string> _domainNames = [];

  public string EmailAddress { get; set; } = "";
  public IReadOnlyList<string> DomainNames => _domainNames;
  public bool AcceptTermsOfService { get; set; } = true;
  public ChallengeType ChallengeType { get; set; } = ChallengeType.Http01;
  public string AcmeDirectoryUrl { get; set; } = "https://acme-v02.api.letsencrypt.org/directory";
  public string CertificateDirectory { get; set; } = "./certificates";

  public void SetDomainNames(IEnumerable<string> domainNames) {
    _domainNames = domainNames.Distinct().ToList();
  }
}

/// <summary>
/// Configuration source for integrating ACME options with IConfiguration.
/// </summary>
public class AcmeOptionsConfigurationSource : IConfigurationSource {
  private readonly IAcmeOptionsProvider _provider;

  public AcmeOptionsConfigurationSource(IAcmeOptionsProvider provider) {
    _provider = provider;
  }

  public IConfigurationProvider Build(IConfigurationBuilder builder) {
    return new AcmeOptionsConfigurationProvider(_provider);
  }
}

/// <summary>
/// Configuration provider for ACME options.
/// </summary>
public class AcmeOptionsConfigurationProvider : ConfigurationProvider {
  private readonly IAcmeOptionsProvider _provider;

  public AcmeOptionsConfigurationProvider(IAcmeOptionsProvider provider) {
    _provider = provider;
  }

  public override void Load() {
    Data["Acme:EmailAddress"] = _provider.EmailAddress;
    Data["Acme:AcceptTermsOfService"] = _provider.AcceptTermsOfService.ToString();
    Data["Acme:ChallengeType"] = _provider.ChallengeType.ToString();
    Data["Acme:AcmeDirectoryUrl"] = _provider.AcmeDirectoryUrl;
    Data["Acme:CertificateDirectory"] = _provider.CertificateDirectory;

    for (int i = 0; i < _provider.DomainNames.Count; i++) {
      Data[$"Acme:DomainNames:{i}"] = _provider.DomainNames[i];
    }
  }
}

/// <summary>
/// Extension methods for adding ACME options provider to configuration.
/// </summary>
public static class AcmeOptionsProviderExtensions {
  public static IConfigurationBuilder AddAcmeOptionsProvider(
    this IConfigurationBuilder builder,
    IAcmeOptionsProvider provider) {
    return builder.Add(new AcmeOptionsConfigurationSource(provider));
  }
}
