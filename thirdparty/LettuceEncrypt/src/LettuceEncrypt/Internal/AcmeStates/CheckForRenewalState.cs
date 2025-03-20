// Copyright (c) Nate McMaster.
// Copyright (c) Fallenwood.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LettuceEncrypt.Internal.AcmeStates;

using LettuceEncrypt.Internal.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class CheckForRenewalState(
    AcmeStateMachineContext context,
    ILogger<CheckForRenewalState> logger,
    IOptionsMonitor<LettuceEncryptOptions> options,
    CertificateSelector selector,
    IClock clock,
    IConfiguration configuration) : AcmeState(context) {
  private CancellationTokenSource _configurationChangeCancellationTokenSource = new();

  private void OnChange() {
    var oldTokenSource = Interlocked.Exchange(ref this._configurationChangeCancellationTokenSource, new());
    oldTokenSource.Cancel();
    oldTokenSource.Dispose();
  }

  public override async Task<IAcmeState> MoveNextAsync(CancellationToken cancellationToken) {
    options.OnChange(_ => {
      this.OnChange();
    });

    while (!cancellationToken.IsCancellationRequested) {
      var checkPeriod = options.CurrentValue.RenewalCheckPeriod;
      var daysInAdvance = options.CurrentValue.RenewDaysInAdvance;
      if (!checkPeriod.HasValue || !daysInAdvance.HasValue) {
        logger.LogWarning("Automatic certificate renewal is not configured. Stopping {service}",
            nameof(AcmeCertificateLoader));
        return MoveTo<TerminalState>();
      }

      var domainNames = options.CurrentValue.DomainNames;
      if (logger.IsEnabled(LogLevel.Debug)) {
        logger.LogDebug("Checking certificates' renewals for {hostname}",
            string.Join(", ", domainNames));

        foreach (var c in configuration.AsEnumerable()) {
          if (c.Key.StartsWith("LettuceEncrypt")) {
            logger.LogDebug("Configuration: {key}={value}", c.Key, c.Value);
          }
        }
      }

      foreach (var domainName in domainNames) {
        if (!selector.TryGet(domainName, out var cert)
            || cert == null
            || cert.NotAfter <= clock.Now.DateTime + daysInAdvance.Value) {
          return MoveTo<BeginCertificateCreationState>();
        }
      }

      var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
          this._configurationChangeCancellationTokenSource.Token,
          cancellationToken);

      try {
        await Task.Delay(checkPeriod.Value, linkedTokenSource.Token);
      } catch (TaskCanceledException) {
        if (logger.IsEnabled(LogLevel.Information)) {
          logger.LogInformation("IOptionsMonitor<LettuceEncryptOptions> changed: {hosts}", string.Join(",", options.CurrentValue.DomainNames));
        }
      }
    }

    return MoveTo<TerminalState>();
  }
}
