// Copyright (c) Fallenwood.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LettuceEncrypt;

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

/// <summary>
/// The LettuceEncrypt options provider.
/// Should be added by user with <see cref="ConfigurationManagerExtensions.AddLettuceEncryptOptionsProvider(Microsoft.Extensions.Configuration.ConfigurationManager, ILettuceEncryptOptionsProvider)"/>
/// </summary>
public interface ILettuceEncryptOptionsProvider : IConfigurationProvider {
  /// <summary>
  /// Set the domain names.
  /// Note: it will replace the existing domain names.
  /// </summary>
  /// <param name="addresses"></param>
  public void SetDomainNames(string[] addresses);

  /// <inheritdoc />
  public IChangeToken ChangeToken { get; }
}

/// <summary>
/// The LettuceEncrypt options provider, supports set domain names dynamically.
/// </summary>
public sealed class LettuceEncryptOptionsProvider() : ConfigurationProvider, ILettuceEncryptOptionsProvider {
  private readonly CancellationTokenSource _configurationChangeTokenSource = new();

  ///<inheritdoc />
  public IChangeToken ChangeToken => new CancellationChangeToken(_configurationChangeTokenSource.Token);

  ///<inheritdoc />
  public void SetDomainNames(string[] domainNames) {
    this.Data = new Dictionary<string, string?>();
    for (var i = 0; i < domainNames.Length; i++) {
      this.Data[$"LettuceEncrypt:DomainNames:{i}"] = domainNames[i];
    }

    this.OnReload();
  }
}
