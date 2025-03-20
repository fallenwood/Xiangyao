// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace LettuceEncrypt.Internal;

internal class FileSystemCertificateRepository : ICertificateRepository, ICertificateSource
{
    private readonly DirectoryInfo _certDir;

    public FileSystemCertificateRepository(DirectoryInfo directory, string? pfxPassword)
    {
        RootDir = directory;
        PfxPassword = pfxPassword;
        _certDir = directory.CreateSubdirectory("certs");
    }

    public DirectoryInfo RootDir { get; }
    public string? PfxPassword { get; }

    public Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        var files = _certDir.GetFiles("*.pfx");
        var certs = new List<X509Certificate2>(files.Length);
        foreach (var file in files)
        {
#if NET9_0_OR_GREATER
      var cert = X509CertificateLoader.LoadPkcs12FromFile(
                path: file.FullName,
                password: PfxPassword);
#else
      var cert = new X509Certificate2(
                fileName: file.FullName,
                password: PfxPassword);
#endif
      certs.Add(cert);
    }

        return Task.FromResult(certs.AsEnumerable());
    }

    public Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        _certDir.Create();

        var tmpFile = Path.GetTempFileName();
        File.WriteAllBytes(
            tmpFile,
            certificate.Export(X509ContentType.Pfx, PfxPassword));

        var fileName = certificate.Thumbprint + ".pfx";
        var output = Path.Combine(_certDir.FullName, fileName);

        // File.Move is an atomic operation on most operating systems. By writing to a temporary file
        // first and then moving it, it avoids potential race conditions with readers.

        File.Move(tmpFile, output);

        return Task.CompletedTask;
    }
}
