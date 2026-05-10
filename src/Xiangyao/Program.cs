using Xiangyao;

using Yarp.ReverseProxy.Configuration;
using Xiangyao.Acme;
using ConsoleAppFramework;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

using Options = Xiangyao.Options;
using Xiangyao.Certificate;
using Xiangyao.Telemetry;
using Microsoft.AspNetCore.ResponseCompression;
using Xiangyao.Utils;

public static partial class Program {

  const string ServiceName = "XiangyaoProxy";

  public static void Main(string[] args) {
    ThreadPool.SetMinThreads(workerThreads: Environment.ProcessorCount * 30, completionPortThreads: 0);
    ConsoleApp.RunAsync(args, PreMainAsync).GetAwaiter().GetResult();
  }

  /// <summary>
  /// Copyright (C) 2023 - 2025 Fallenwood
  /// </summary>
  /// <param name="provider">-p, Config Provider (e.g. None, File, Docker, etc.)</param>
  /// <param name="useHttps">--https, Use HTTPS</param>
  /// <param name="useHttpsRedirect">--https-redirect, Use HTTPS Redirect</param>
  /// <param name="useLetsEncrypt">--lets-encrypt, Use Let's Encrypt</param>
  /// <param name="useZeroSsl">--zero-ssl, Use ZeroSSL</param>
  /// <param name="acmeEmail">ACME account email address</param>
  /// <param name="acmeDomainNames">ACME certificate domain names</param>
  /// <param name="zeroSslEabKid">Optional ZeroSSL EAB Key Identifier</param>
  /// <param name="zeroSslEabHmacKey">Optional ZeroSSL EAB HMAC Key</param>
  /// <param name="useOtel">Use Opentelemetry</param>
  /// <param name="otelLogEndpoint">--otel-log, Opentelemetry Logs Endpoint</param>
  /// <param name="otelTraceEndpoint">--otel-trace, Opentelemetry Trace Endpoint</param>
  /// <param name="otelMeterEndpoint">--otel-meter, Opentelemetry Meter Endpoint</param>
  /// <param name="certificate">--certificate-path, The fullchain.pem</param>
  /// <param name="certificateKey">--certificate-key-path, The privkey.pem</param>
  /// <param name="usePortal">--portal,--enable-portal,Enable portal</param>
  /// <param name="portalPort">Portal port</param>
  static async Task PreMainAsync(
    Provider provider = Provider.Docker,
    bool useHttps = false,
    bool useHttpsRedirect = false,
    bool useLetsEncrypt = false,
    bool useZeroSsl = false,
    string acmeEmail = "",
    string[]? acmeDomainNames = null,
    string zeroSslEabKid = "",
    string zeroSslEabHmacKey = "",
    bool useOtel = false,
    string otelLogEndpoint = "http://localhost:4317",
    string otelTraceEndpoint = "http://localhost:4317",
    string otelMeterEndpoint = "http://localhost:4317",
    string certificate = "",
    string certificateKey = "",
    bool usePortal = false,
    int portalPort = 8080) {
    if (useLetsEncrypt && useZeroSsl) {
      throw new ArgumentException("Use either --lets-encrypt or --zero-ssl, not both.");
    }

    var certificateAuthority = useZeroSsl
      ? AcmeCertificateAuthority.ZeroSsl
      : AcmeCertificateAuthority.LetsEncrypt;

    var options = new Options(
      AcmeDomainNames: acmeDomainNames ?? [],
      Provider: provider,
      UseAcmeCertificates: useLetsEncrypt || useZeroSsl,
      UseHttps: useHttps,
      UseHttpsRedirect: useHttpsRedirect,
      AcmeEmailAddress: acmeEmail,
      CertificateAuthority: certificateAuthority,
      ExternalAccountBinding: CreateExternalAccountBindingOptions(
        certificateAuthority,
        zeroSslEabKid,
        zeroSslEabHmacKey),
      Certificate: certificate,
      CertificateKey: certificateKey,
      UseOtel: useOtel,
      OtelLogEndpoint: otelLogEndpoint,
      OtelTraceEndpoint: otelTraceEndpoint,
      OtelMeterEndpoint: otelMeterEndpoint,
      UsePortal: usePortal,
      PortalPort: portalPort);

    await MainAsync(options);
  }

  /// <param
  static async Task MainAsync(Options options) {
    var domainProvider = new AcmeDomainProvider();

    var builder = WebApplication.CreateSlimBuilder([]);

    builder.Services.AddSingleton<IAcmeDomainProvider>(domainProvider);

    switch (options.Provider) {
      case Provider.None:
        AddNoopServices(builder);
        break;
      case Provider.File:
        AddFileServices(builder);
        break;
      case Provider.Docker:
        AddDockerServices(builder);
        break;
    }

    if (options.UseHttps) {
      if (options.UseAcmeCertificates) {
        var authorityName = GetAcmeAuthorityDisplayName(options.CertificateAuthority);
        Console.WriteLine($"Use {authorityName} with {options.AcmeEmailAddress} for {string.Join(",", options.AcmeDomainNames)}");

        domainProvider.SetDomainNames(options.AcmeDomainNames);

        var certificateDirectory = Path.Join(Directory.GetCurrentDirectory(), GetAcmeCertificateDirectoryName(options.CertificateAuthority));
        var acmeDirectoryUrl = AcmeDirectoryUrls.GetDirectoryUrl(options.CertificateAuthority);

        builder.Services.AddHttpClient();
        builder.Services.AddAcmeHttp01Challenge();

        builder.Services.AddSingleton<AcmeCertificateHostedService>(sp =>
          new AcmeCertificateHostedService(
            sp.GetRequiredService<IAcmeDomainProvider>(),
            sp.GetRequiredService<IHttp01ChallengeStore>(),
            sp.GetRequiredService<IHttpClientFactory>(),
            options.AcmeEmailAddress,
            certificateDirectory,
            acmeDirectoryUrl,
            options.ExternalAccountBinding,
            sp.GetRequiredService<ILogger<AcmeCertificateHostedService>>()));

        builder.Services.AddHostedService(sp => sp.GetRequiredService<AcmeCertificateHostedService>());

        builder.WebHost.UseKestrel(kestrel => {
          AcmeCertificateHostedService? certificateService = null;

          kestrel.ConfigureHttpsDefaults(h => {
            certificateService ??= kestrel.ApplicationServices.GetRequiredService<AcmeCertificateHostedService>();
            h.ServerCertificateSelector = (_, _) => certificateService!.Certificate;
          });

          kestrel.ConfigureEndpointDefaults(e => {
            e.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
          });
        });
      } else {
        Console.WriteLine($"Use existing certificate");

        var certificateSelector = new WildcastServerCertificateSelector(
          options.Certificate,
          options.CertificateKey);

        builder.WebHost.UseKestrel(kestrel => {
          kestrel.ConfigureHttpsDefaults(h => {
            h.ServerCertificateSelector = (context, domainName) =>
              certificateSelector.Select(context!, domainName);
          });

          kestrel.ConfigureEndpointDefaults(e => {
            e.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
          });
        });
      }
    }

    if (options.UseOtel) {
      Console.WriteLine($"Enabled Opentelemetry over OTLP/gRPC, trace:{options.OtelTraceEndpoint}, logs:{options.OtelLogEndpoint}, meter:{options.OtelMeterEndpoint}");

      builder.Logging.AddOpenTelemetry(logging => {
        logging
          .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
              .AddService(ServiceName));

        if (!string.IsNullOrEmpty(options.OtelLogEndpoint)) {
          logging.AddOtlpExporter(exporter => {
            ConfigureOtlpGrpcExporter(exporter, options.OtelLogEndpoint);
          });
        }
      });

      builder.Services.AddOpenTelemetryMeter();

      builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(ServiceName))
        .WithTracing(tracing => {
          tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(ServiceName);

          if (!string.IsNullOrEmpty(options.OtelTraceEndpoint)) {
            tracing.AddOtlpExporter(exporter => {
              ConfigureOtlpGrpcExporter(exporter, options.OtelTraceEndpoint);
            });
          }
        })
        .WithMetrics(metrics => {
          metrics
            .AddMeter(OpenTelemetryMeterProvider.Name)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();

          if (!string.IsNullOrEmpty(options.OtelMeterEndpoint)) {
            metrics.AddOtlpExporter(exporter => {
              ConfigureOtlpGrpcExporter(exporter, options.OtelMeterEndpoint);
            });
          }

          if (builder.Environment.IsDevelopment()) {
            metrics.AddConsoleExporter();
          }
        });
    }

    builder.Services.AddResponseCompression(options => {
      options.EnableForHttps = true;
      options.Providers.Add<ZstdCompressionProvider>();
      options.Providers.Add<BrotliCompressionProvider>();
      options.Providers.Add<DeflateCompressionProvider>();
      options.Providers.Add<GzipCompressionProvider>();
    });

    var app = builder.Build();

    if (options.UseAcmeCertificates) {
      app.UseAcmeHttp01Challenge();
    }

    if (options.UseHttpsRedirect) {
      app.UseHttpsRedirection();
    }

    app.MapReverseProxy();

    app.UseResponseCompression();

    if (options.UsePortal) {
      var portal = new Portal(options.PortalPort);

      portal.ConfigureServices(app.Services);
      portal.Configure();

      await Task.WhenAll(app.RunAsync(), portal.RunAsync());
    } else {
      await app.RunAsync();
    }
  }

  static void ConfigureOtlpGrpcExporter(OtlpExporterOptions exporter, string endpoint) {
    exporter.Endpoint = new(endpoint);
    exporter.Protocol = OtlpExportProtocol.Grpc;
  }

  static AcmeExternalAccountBindingOptions? CreateExternalAccountBindingOptions(
    AcmeCertificateAuthority certificateAuthority,
    string zeroSslEabKid,
    string zeroSslEabHmacKey) {
    if (certificateAuthority != AcmeCertificateAuthority.ZeroSsl) {
      return null;
    }

    var hasKeyIdentifier = !string.IsNullOrWhiteSpace(zeroSslEabKid);
    var hasHmacKey = !string.IsNullOrWhiteSpace(zeroSslEabHmacKey);

    if (!hasKeyIdentifier && !hasHmacKey) {
      return null;
    }

    if (hasKeyIdentifier != hasHmacKey) {
      throw new ArgumentException("Provide both --zero-ssl-eab-kid and --zero-ssl-eab-hmac-key, or omit both to obtain them from ZeroSSL using the account email.");
    }

    return new AcmeExternalAccountBindingOptions(zeroSslEabKid, zeroSslEabHmacKey);
  }

  static string GetAcmeAuthorityDisplayName(AcmeCertificateAuthority certificateAuthority) {
    return certificateAuthority switch {
      AcmeCertificateAuthority.LetsEncrypt => "Let's Encrypt",
      AcmeCertificateAuthority.ZeroSsl => "ZeroSSL",
      _ => throw new ArgumentOutOfRangeException(nameof(certificateAuthority), certificateAuthority, "Unsupported ACME certificate authority"),
    };
  }

  static string GetAcmeCertificateDirectoryName(AcmeCertificateAuthority certificateAuthority) {
    return certificateAuthority switch {
      AcmeCertificateAuthority.LetsEncrypt => "letsencrypt",
      AcmeCertificateAuthority.ZeroSsl => "zerossl",
      _ => throw new ArgumentOutOfRangeException(nameof(certificateAuthority), certificateAuthority, "Unsupported ACME certificate authority"),
    };
  }

  static void AddNoopServices(WebApplicationBuilder builder) {
    builder.Services.AddReverseProxy()
      .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
      .ConfigureUnixSocket();

    builder.Services.AddSingleton<IXiangyaoProxyConfigProvider, FileProxyConfigProvider>();
  }

  static void AddFileServices(WebApplicationBuilder builder) {
    builder.Configuration.AddJsonFile("xiangyao.json", optional: false, reloadOnChange: true);

    builder.Services.AddReverseProxy()
      .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
      .ConfigureUnixSocket();

    builder.Services.AddSingleton<IXiangyaoProxyConfigProvider, FileProxyConfigProvider>();
  }

  static void AddDockerServices(WebApplicationBuilder builder) {
    builder.Services.AddReverseProxy()
      .ConfigureUnixSocket();

    builder.Services.AddSingleton<DockerProxyConfigProvider>();
    builder.Services.AddSingleton<DockerSocket>(_ => new DockerSocket.DockerUnixDomainSocket("/run/docker.sock"));
    builder.Services.AddSingleton<IDockerProvider, DockerProvider>();
    builder.Services.AddSingleton<IProxyConfigProvider, DockerProxyConfigProvider>(sp => sp.GetRequiredService<DockerProxyConfigProvider>());
    builder.Services.AddSingleton<IXiangyaoProxyConfigProvider, DockerProxyConfigProvider>(sp => sp.GetRequiredService<DockerProxyConfigProvider>());
    builder.Services.AddSingleton<IUpdateConfig, DockerProxyConfigProvider>(sp => sp.GetRequiredService<DockerProxyConfigProvider>());
    builder.Services.AddSingleton<ILabelParser, SwitchCaseLabelParser>();

    builder.Services.AddHostedService<DockerMonitorHostedService>();
  }
}
