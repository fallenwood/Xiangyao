namespace Xiangyao.Acme;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

public static class AcmeExtensions {
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

