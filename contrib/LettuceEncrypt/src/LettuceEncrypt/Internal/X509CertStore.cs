// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LettuceEncrypt.Internal;

internal class X509CertStore : ICertificateSource, ICertificateRepository, IDisposable
{
    private readonly X509Store _store;
    private readonly IOptionsMonitor<LettuceEncryptOptions> _options;
    private readonly ILogger<X509CertStore> _logger;
    private readonly IConfiguration _configuration;

    public bool AllowInvalidCerts { get; set; }

    public X509CertStore(IOptionsMonitor<LettuceEncryptOptions> options, ILogger<X509CertStore> logger, IConfiguration configuration)
    {
        _store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        _store.Open(OpenFlags.ReadWrite);
        _options = options;
        _logger = logger;
        this._configuration = configuration;
    }

    public Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        var domainNames = new HashSet<string>(_options.CurrentValue.DomainNames);
        var result = new List<X509Certificate2>();
        var certs = _store.Certificates.Find(X509FindType.FindByTimeValid,
            DateTime.Now,
            validOnly: !AllowInvalidCerts);

        foreach (var cert in certs)
        {
            if (!cert.HasPrivateKey)
            {
                continue;
            }

            foreach (var dnsName in X509CertificateHelpers.GetAllDnsNames(cert))
            {
                if (domainNames.Contains(dnsName))
                {
                    result.Add(cert);
                    break;
                }
            }
        }

        return Task.FromResult(result.AsEnumerable());
    }

    public Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        try
        {
            _store.Add(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(0, ex, "Failed to save certificate to store");
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _store.Close();
    }
}
