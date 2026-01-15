namespace Xiangyao.Acme;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

public static class AcmeExtensions {
  /// <summary>
  /// Adds ACME services with HTTP-01 challenge support.
  /// </summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configure">Optional configuration action.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddAcme(
    this IServiceCollection services,
    Action<AcmeOptionsProvider>? configure = null) {
    var optionsProvider = new AcmeOptionsProvider();
    configure?.Invoke(optionsProvider);

    services.AddSingleton<IAcmeOptionsProvider>(optionsProvider);
    services.AddSingleton<IHttp01ChallengeStore, Http01ChallengeStore>();
    services.AddSingleton<AcmeCertificateRenewalService>();
    services.AddHostedService(sp => sp.GetRequiredService<AcmeCertificateRenewalService>());

    return services;
  }

  /// <summary>
  /// Adds ACME services with a pre-configured options provider.
  /// </summary>
  public static IServiceCollection AddAcme(
    this IServiceCollection services,
    IAcmeOptionsProvider optionsProvider) {
    services.AddSingleton(optionsProvider);
    services.AddSingleton<IHttp01ChallengeStore, Http01ChallengeStore>();
    services.AddSingleton<AcmeCertificateRenewalService>();
    services.AddHostedService(sp => sp.GetRequiredService<AcmeCertificateRenewalService>());

    return services;
  }

  public static IServiceCollection AddAcmeHttp01Challenge(this IServiceCollection services) {
    services.AddSingleton<IHttp01ChallengeStore, Http01ChallengeStore>();
    return services;
  }

  public static IServiceCollection AddAcmeDns01Challenge(this IServiceCollection services) {
    services.AddSingleton<IDns01ChallengeStore, Dns01ChallengeStore>();
    return services;
  }

  public static IServiceCollection AddAcmeTlsAlpn01Challenge(this IServiceCollection services) {
    services.AddSingleton<ITlsAlpn01ChallengeStore, TlsAlpn01ChallengeStore>();
    return services;
  }

  public static IServiceCollection AddAcmeChallenges(this IServiceCollection services) {
    services.AddAcmeHttp01Challenge();
    services.AddAcmeDns01Challenge();
    services.AddAcmeTlsAlpn01Challenge();
    return services;
  }

  public static IApplicationBuilder UseAcmeHttp01Challenge(this IApplicationBuilder app) {
    app.Use(async (context, next) => {
      if (context.Request.Path.StartsWithSegments("/.well-known/acme-challenge")) {
        var token = context.Request.Path.Value?.Split('/').LastOrDefault();
        if (token != null) {
          var store = context.RequestServices.GetRequiredService<IHttp01ChallengeStore>();
          var keyAuth = store.GetKeyAuthorization(token);
          if (keyAuth != null) {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(keyAuth);
            return;
          }
        }
        context.Response.StatusCode = 404;
        return;
      }
      await next();
    });
    return app;
  }
}

