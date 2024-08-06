namespace Xiangyao.Telemetry;

using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;

internal static partial class OpenTelemetryMeter {
  [Counter<int>()]
  internal static partial DockerHit DockerHit(this Meter meter);

  [Counter<int>()]
  internal static partial DockerMiss DockerMiss(this Meter meter);
}

public class OpenTelemetryMeterProvider(IMeterFactory factory, IServiceProvider serviceProvider) {
  public Meter Meter { get; } = factory.Create("Xiangyao");

  internal DockerHit DockerHit => serviceProvider.GetRequiredService<DockerHit>();
  internal DockerMiss DockerMiss => serviceProvider.GetRequiredService<DockerMiss>();

  public void RecordDockerHit() => DockerHit.Add(1);
  public void RecordDockerMiss() => DockerMiss.Add(1);
}

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddOpenTelemetryMeter(this IServiceCollection services) {
    services.AddSingleton<OpenTelemetryMeterProvider>();

    services.AddSingleton(sp => {
      var mp = sp.GetRequiredService<OpenTelemetryMeterProvider>();
      return mp.Meter.DockerHit();
    });

    services.AddSingleton(sp => {
      var mp = sp.GetRequiredService<OpenTelemetryMeterProvider>();
      return mp.Meter.DockerMiss();
    });

    return services;
  }
}
