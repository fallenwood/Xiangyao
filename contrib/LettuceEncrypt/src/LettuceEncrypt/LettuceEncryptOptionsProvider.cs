// Copyright (c) Fallenwood.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LettuceEncrypt;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

public interface ILettuceEncryptOptionsProvider : IConfigurationProvider {
  public void SetAddresses(string[] addresses);

  public IChangeToken ChangeToken { get; }
}

public sealed class LettuceEncryptOptionsProvider : ConfigurationProvider, ILettuceEncryptOptionsProvider {
  private readonly CancellationTokenSource _configurationChangeTokenSource = new();

  public IChangeToken ChangeToken => new CancellationChangeToken(_configurationChangeTokenSource.Token);

  public void SetAddresses(string[] addresses) {
    this.Data = new Dictionary<string, string?>();
    for (var i = 0; i < addresses.Length; i++) {
      this.Data[$"LettuceEncrypt:DomainNames:{i}"] = addresses[i];
    }

    this.OnReload();
  }
}

public class LettuceEncryptOptionsConfigurationSource(ILettuceEncryptOptionsProvider provider) : IConfigurationSource {
  public IConfigurationProvider Build(IConfigurationBuilder builder) {
    return provider;
  }
}

public static class ServiceCollections {
  public static ConfigurationManager AddLettuceEncryptOptionsProvider(
      this ConfigurationManager manager, ILettuceEncryptOptionsProvider provider) {
    IConfigurationBuilder configBuilder = manager;
    configBuilder.Add(new LettuceEncryptOptionsConfigurationSource(provider));

    return manager;
  }
}
