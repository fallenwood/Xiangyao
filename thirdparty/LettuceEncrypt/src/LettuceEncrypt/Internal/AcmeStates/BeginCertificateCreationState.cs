// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LettuceEncrypt.Internal.AcmeStates;

internal class BeginCertificateCreationState(
    AcmeStateMachineContext context,
    ILogger<ServerStartupState> logger,
    IOptionsMonitor<LettuceEncryptOptions> options,
    AcmeCertificateFactory acmeCertificateFactory,
    CertificateSelector selector,
    IEnumerable<ICertificateRepository> certificateRepositories) : AcmeState(context)
{
  public override async Task<IAcmeState> MoveNextAsync(CancellationToken cancellationToken)
    {
        var domainNames = options.CurrentValue.DomainNames;

        try
        {
            var account = await acmeCertificateFactory.GetOrCreateAccountAsync(cancellationToken);
            logger.LogInformation("Using account {accountId}", account.Id);

            logger.LogInformation("Creating certificate for {hostname}",
                string.Join(",", domainNames));

            if (domainNames.Length == 0)
            {
                logger.LogInformation("No domain names are configured. Skipping certificate creation.");
                return MoveTo<CheckForRenewalState>();
            }

            var cert = await acmeCertificateFactory.CreateCertificateAsync(cancellationToken);

            logger.LogInformation("Created certificate {subjectName} ({thumbprint})",
                cert.Subject,
                cert.Thumbprint);

            await SaveCertificateAsync(cert, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(0, ex, "Failed to automatically create a certificate for {hostname}", domainNames);
            throw;
        }

        return MoveTo<CheckForRenewalState>();
    }

    private async Task SaveCertificateAsync(X509Certificate2 cert, CancellationToken cancellationToken)
    {
        selector.Add(cert);

        var saveTasks = new List<Task>
        {
            Task.Delay(TimeSpan.FromMinutes(5), cancellationToken)
        };

        var errors = new List<Exception>();
        foreach (var repo in certificateRepositories)
        {
            try
            {
                saveTasks.Add(repo.SaveAsync(cert, cancellationToken));
            }
            catch (Exception ex)
            {
                // synchronous saves may fail immediately
                errors.Add(ex);
            }
        }

        await Task.WhenAll(saveTasks);

        if (errors.Count > 0)
        {
            throw new AggregateException("Failed to save cert to repositories", errors);
        }
    }
}
