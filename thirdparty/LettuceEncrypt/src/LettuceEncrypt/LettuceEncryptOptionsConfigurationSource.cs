// Copyright (c) Fallenwood.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LettuceEncrypt;

using Microsoft.Extensions.Configuration;

internal class LettuceEncryptOptionsConfigurationSource(ILettuceEncryptOptionsProvider provider) : IConfigurationSource {
  public IConfigurationProvider Build(IConfigurationBuilder builder) {
    return provider;
  }
}
