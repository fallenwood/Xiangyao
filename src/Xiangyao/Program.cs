using Xiangyao;

using Yarp.ReverseProxy.Configuration;
using System.CommandLine;
using LettuceEncrypt;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

using Options = Xiangyao.Options;
using Xiangyao.Certificate;
using Xiangyao.Telemetry;

const string ServiceName = "XiangyaoProxy";

var bindings = new OptionBindings();

var rootCommand = new RootCommand($"Copyright (C) 2023 - {DateTime.UtcNow.Year} Fallenwood");
rootCommand.AddOption(bindings.providerOption);
rootCommand.AddOption(bindings.useHttps);
rootCommand.AddOption(bindings.useHttpsRedirect);
rootCommand.AddOption(bindings.useLetsEncrypt);
rootCommand.AddOption(bindings.letsEncryptDomainNames);
rootCommand.AddOption(bindings.letsEncryptEmailAddress);
rootCommand.AddOption(bindings.useOtel);
rootCommand.AddOption(bindings.otelLogEndpoint);
rootCommand.AddOption(bindings.otelTraceEndpoint);
rootCommand.AddOption(bindings.otelMeterEndpoint);
rootCommand.AddOption(bindings.certificate);
rootCommand.AddOption(bindings.certificateKey);
rootCommand.AddOption(bindings.usePortal);
rootCommand.AddOption(bindings.portalPort);

rootCommand.Handler = new CustomHandler(
  bindings,
  async (context, options) => {
    await MainAsync(args, options);
  });

await rootCommand.InvokeAsync(args);

async Task MainAsync(string[] args, Options options) {
  var provider = new LettuceEncryptOptionsProvider();

  var builder = WebApplication.CreateSlimBuilder(args);

  builder.Configuration.AddLettuceEncryptOptionsProvider(provider);

  builder.Services.AddSingleton<ILettuceEncryptOptionsProvider>(provider);

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
    if (options.UseLetsEncrypt) {
      Console.WriteLine($"Use LetsEncrypt with {options.LetsEncryptEmailAddress} for {string.Join(",", options.LetsEncryptDomainNames)}");

      builder
        .Services
        .AddLettuceEncrypt(
          o => {
            o.AcceptTermsOfService = true;
            o.EmailAddress = options.LetsEncryptEmailAddress;
            o.DomainNames = options.LetsEncryptDomainNames;
            o.AllowedChallengeTypes = LettuceEncrypt.Acme.ChallengeType.Http01;
          },
          builder.Configuration)
        .PersistDataToDirectory(new(Path.Join(Directory.GetCurrentDirectory(), "letsencrypt")), pfxPassword: null);

      builder.WebHost.UseKestrel(kestrel => {
        kestrel.ConfigureHttpsDefaults(h => {
          h.UseLettuceEncrypt(kestrel.ApplicationServices);
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
          h.UseServerCertificateSelector(certificateSelector);
        });

        kestrel.ConfigureEndpointDefaults(e => {
          e.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
        });
      });
    }
  }

  if (options.UseOtel) {
    Console.WriteLine($"Enabled Opentelemetry, trace:{options.OtelTraceEndpoint}, logs:{options.OtelLogEndpoint}, meter:{options.OtelMeterEndpoint}");

    builder.Logging.AddOpenTelemetry(logging => {
      logging
        .SetResourceBuilder(
          ResourceBuilder.CreateDefault()
            .AddService(ServiceName));

      if (!string.IsNullOrEmpty(options.OtelLogEndpoint)) {
        logging.AddOtlpExporter(exporter => {
          exporter.Endpoint = new(options.OtelLogEndpoint);
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
            exporter.Endpoint = new(options.OtelTraceEndpoint);
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
            exporter.Endpoint = new(options.OtelMeterEndpoint);
          });
        }

        if (builder.Environment.IsDevelopment()) {
          metrics.AddConsoleExporter();
        }
      });
  }

  var app = builder.Build();

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

void AddNoopServices(WebApplicationBuilder builder) {
  builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

  builder.Services.AddSingleton<IXiangyaoProxyConfigProvider, FileProxyConfigProvider>();
}

void AddFileServices(WebApplicationBuilder builder) {
  builder.Configuration.AddJsonFile("xiangyao.json", optional: false, reloadOnChange: true);

  builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

  builder.Services.AddSingleton<IXiangyaoProxyConfigProvider, FileProxyConfigProvider>();
}

void AddDockerServices(WebApplicationBuilder builder) {
  builder.Services.AddReverseProxy();

  builder.Services.AddSingleton<DockerProxyConfigProvider>();
  builder.Services.AddSingleton<DockerSocket>(_ => new DockerSocket.DockerUnixDomainSocket("/run/docker.sock"));
  builder.Services.AddSingleton<IDockerProvider, DockerProvider>();
  builder.Services.AddSingleton<IProxyConfigProvider, DockerProxyConfigProvider>(sp => sp.GetRequiredService<DockerProxyConfigProvider>());
  builder.Services.AddSingleton<IXiangyaoProxyConfigProvider, DockerProxyConfigProvider>(sp => sp.GetRequiredService<DockerProxyConfigProvider>());
  builder.Services.AddSingleton<IUpdateConfig, DockerProxyConfigProvider>(sp => sp.GetRequiredService<DockerProxyConfigProvider>());
  builder.Services.AddSingleton<ILabelParser, SwitchCaseLabelParser>();

  builder.Services.AddHostedService<DockerMonitorHostedService>();
}
