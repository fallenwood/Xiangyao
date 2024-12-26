namespace Xiangyao;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xiangyao.Common;

public class Portal {
  internal const string AppKey = "Xiangyao";

  public WebApplicationBuilder Builder { get; private set; } = default!;
  public WebApplication App { get; private set; } = default!;

  public Portal(int port) {
    var builder = WebApplication.CreateSlimBuilder([]);
    this.Builder = builder;

    builder.WebHost.ConfigureKestrel(kestrel => {
      kestrel.ListenAnyIP(port);
    });
  }

  public void ConfigureServices(IServiceProvider appServiceProvider) {
    this.Builder.Services.AddKeyedSingleton<IServiceProvider>(AppKey, appServiceProvider);
    this.Builder.Services.AddHealthChecks();
    this.App = this.Builder.Build();
  }

  public void Configure() {
    var app = this.App;

    app.MapHealthChecks("/healthz");

    var location = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(rnn => rnn.Contains("wwwroot"));

    StaticFileOptions staticFileOptions = new () {
      FileProvider = new ManifestEmbeddedFileProvider(typeof(Portal).Assembly, "wwwroot"),
    };
    
    app.MapFallbackToFile("index.html", staticFileOptions);

    var api = app.MapGroup("/api");

    api.MapGet("configuration", async (HttpContext context) => {
      var appServiceProvider = context.RequestServices.GetRequiredKeyedService<IXiangyaoProxyConfigProvider>(AppKey);

      await context.Response.WriteAsJsonAsync(appServiceProvider.Config, XiangyaoJsonContext.Default.XiangyaoProxyConfig);
    });

    app.UseStaticFiles(staticFileOptions);
  }

  public Task RunAsync() {
    return this.App.RunAsync();
  }
}
