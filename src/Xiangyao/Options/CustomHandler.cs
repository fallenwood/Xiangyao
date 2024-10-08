namespace Xiangyao;

using System.CommandLine.Invocation;

internal sealed class CustomHandler(
  OptionBindings bindings,
  Func<InvocationContext, Options, Task>? asyncHandler,
  Action<InvocationContext, Options>? syncHandler
  ) : ICommandHandler {

  public CustomHandler(OptionBindings bindings, Func<InvocationContext, Options, Task> handler)
    : this(bindings, asyncHandler: handler, syncHandler: null) { }


  public CustomHandler(OptionBindings bindings, Action<InvocationContext, Options> handler)
    : this(bindings, asyncHandler: null, syncHandler: handler) { }

  public int Invoke(InvocationContext context) {
    if (syncHandler is not null) {
      var options = this.CreateOptions(context);
      syncHandler!(context, options);
      return context.ExitCode;
    }

    return SyncUsingAsync(context);
  }

  private int SyncUsingAsync(InvocationContext context) => InvokeAsync(context).GetAwaiter().GetResult();

  private Options CreateOptions(InvocationContext context) {
    var provider = context.ParseResult.GetValueForOption(bindings.providerOption);
    var useHttps = context.ParseResult.GetValueForOption(bindings.useHttps);
    var useHttpsRedirect = context.ParseResult.GetValueForOption(bindings.useHttpsRedirect);
    var useLetsEncrypt = context.ParseResult.GetValueForOption(bindings.useLetsEncrypt);
    var letsEncryptDomainNames = context.ParseResult.GetValueForOption(bindings.letsEncryptDomainNames);
    var letsEncryptEmailAddress = context.ParseResult.GetValueForOption(bindings.letsEncryptEmailAddress);
    var certificate = context.ParseResult.GetValueForOption(bindings.certificate);
    var certificateKey = context.ParseResult.GetValueForOption(bindings.certificateKey);
    var useOtel = context.ParseResult.GetValueForOption(bindings.useOtel);
    var otelLogEndpoint = context.ParseResult.GetValueForOption(bindings.otelLogEndpoint);
    var otelTraceEndpoint = context.ParseResult.GetValueForOption(bindings.otelTraceEndpoint);
    var otelMeterEndpoint = context.ParseResult.GetValueForOption(bindings.otelMeterEndpoint);
    var usePortal = context.ParseResult.GetValueForOption(bindings.usePortal);
    var portalPort = context.ParseResult.GetValueForOption(bindings.portalPort);

    var options = new Options(
      Provider: provider,
      UseHttps: useHttps,
      UseHttpsRedirect: useHttpsRedirect,
      UseLetsEncrypt: useLetsEncrypt,
      LetsEncryptDomainNames: letsEncryptDomainNames!.ToArray(),
      LetsEncryptEmailAddress: letsEncryptEmailAddress ?? string.Empty,
      Certificate: certificate ?? string.Empty,
      CertificateKey: certificateKey ?? string.Empty,
      UseOtel: useOtel,
      OtelLogEndpoint: otelLogEndpoint ?? string.Empty,
      OtelTraceEndpoint: otelTraceEndpoint ?? string.Empty,
      OtelMeterEndpoint: otelMeterEndpoint ?? string.Empty,
      UsePortal: usePortal,
      PortalPort: portalPort);

    return options;
  }

  public async Task<int> InvokeAsync(InvocationContext context) {
    if (syncHandler is not null) {
      return Invoke(context);
    }

    var options = this.CreateOptions(context);
    await asyncHandler!.Invoke(context, options);

    return context.ExitCode;
  }
}
