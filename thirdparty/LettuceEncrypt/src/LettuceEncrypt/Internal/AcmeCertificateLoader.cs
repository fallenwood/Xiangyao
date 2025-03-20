// Copyright (c) Nate McMaster.
// Copyright (c) Fallenwood.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LettuceEncrypt.Internal.AcmeStates;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LettuceEncrypt.Internal;

/// <summary>
/// This starts the ACME state machine, which handles certificate generation and renewal
/// </summary>
internal class AcmeCertificateLoader(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AcmeCertificateLoader> logger,
    IServer server,
    IConfiguration config) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    if (!server.GetType().Name.StartsWith(nameof(KestrelServer))) {
      var serverType = server.GetType().FullName;
      logger.LogWarning(
          "LettuceEncrypt can only be used with Kestrel and is not supported on {serverType} servers. Skipping certificate provisioning.",
          serverType);
      return;
    }

    if (config.GetValue<bool>("UseIISIntegration")) {
      logger.LogWarning(
          "LettuceEncrypt does not work with apps hosting in IIS. IIS does not allow for dynamic HTTPS certificate binding." +
          "Skipping certificate provisioning.");
      return;
    }

    using var acmeStateMachineScope = serviceScopeFactory.CreateScope();

    try {
      IAcmeState state = acmeStateMachineScope.ServiceProvider.GetRequiredService<ServerStartupState>();

      while (!stoppingToken.IsCancellationRequested) {
        logger.LogTrace("ACME state transition: moving to {stateName}", state.GetType().Name);
        state = await state.MoveNextAsync(stoppingToken);
      }
    } catch (OperationCanceledException) {
      logger.LogDebug("State machine cancellation requested. Exiting...");
    } catch (AggregateException ex) when (ex.InnerException != null) {
      logger.LogError(0, ex.InnerException, "ACME state machine encountered unhandled error");
    } catch (Exception ex) {
      logger.LogError(0, ex, "ACME state machine encountered unhandled error");
    }
  }
}
