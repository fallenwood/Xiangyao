namespace Xiangyao.Acme.Examples;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xiangyao.Acme;
using Xiangyao.Acme.DnsProviders;

public static class UsageExamples {
  // Example 1: HTTP-01 Challenge (original)
  public static async Task Example1_Http01() {
    var client = new AcmeClient();
    var challengeStore = new Http01ChallengeStore();
    var manager = new AcmeCertificateManager(
      client,
      challengeStore,
      "admin@example.com",
      "./certificates");

    var domains = new[] { "example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "example.com");
  }

  // Example 2: DNS-01 Challenge with Manual DNS Provider
  public static async Task Example2_Dns01Manual() {
    var client = new AcmeClient();
    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.Dns01,
      Dns01Store = new Dns01ChallengeStore(),
      DnsProvider = new ManualDnsProvider()
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      "admin@example.com",
      "./certificates",
      options);

    // DNS-01 supports wildcard certificates!
    var domains = new[] { "*.example.com", "example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "wildcard.example.com");
  }

  // Example 3: DNS-01 Challenge with Cloudflare
  public static async Task Example3_Dns01Cloudflare() {
    var client = new AcmeClient();
    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.Dns01,
      Dns01Store = new Dns01ChallengeStore(),
      DnsProvider = new CloudflareDnsProvider(
        apiToken: "your-cloudflare-api-token",
        zoneId: "your-zone-id")
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      "admin@example.com",
      "./certificates",
      options);

    var domains = new[] { "*.example.com", "example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "wildcard.example.com");
  }

  // Example 4: TLS-ALPN-01 Challenge
  public static async Task Example4_TlsAlpn01() {
    var client = new AcmeClient();
    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.TlsAlpn01,
      TlsAlpn01Store = new TlsAlpn01ChallengeStore()
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      "admin@example.com",
      "./certificates",
      options);

    var domains = new[] { "example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "example.com");
  }

  // Example 5: Multi-Challenge Support with Fallback
  public static async Task Example5_MultiChallenge() {
    var client = new AcmeClient();
    
    // Configure all challenge types
    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.Http01,
      Http01Store = new Http01ChallengeStore(),
      Dns01Store = new Dns01ChallengeStore(),
      TlsAlpn01Store = new TlsAlpn01ChallengeStore(),
      DnsProvider = new ManualDnsProvider()
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      "admin@example.com",
      "./certificates",
      options);

    // Will try HTTP-01 first, fallback to DNS-01 or TLS-ALPN-01 if needed
    var domains = new[] { "example.com", "www.example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "example.com");
  }

  // Example 6: ASP.NET Core Integration with HTTP-01
  public static void Example6_AspNetCoreHttp01() {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());

    // Add ACME challenge services
    builder.Services.AddAcmeHttp01Challenge();

    var app = builder.Build();

    // Enable ACME HTTP-01 challenge endpoint
    app.UseAcmeHttp01Challenge();

    app.MapGet("/", () => "Hello World!");
    app.Run();
  }

  // Example 7: ASP.NET Core Integration with TLS-ALPN-01
  public static void Example7_AspNetCoreTlsAlpn01() {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());

    builder.Services.AddAcmeTlsAlpn01Challenge();

    // Note: TLS-ALPN-01 requires custom Kestrel configuration
    // This is a simplified example - production use would need more setup

    var app = builder.Build();
    app.MapGet("/", () => "Hello World!");
    app.Run();
  }

  // Example 8: ASP.NET Core Integration with All Challenges
  public static void Example8_AspNetCoreAllChallenges() {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());

    // Add all challenge types
    builder.Services.AddAcmeChallenges();

    var app = builder.Build();

    // Enable HTTP-01 challenge endpoint
    app.UseAcmeHttp01Challenge();

    app.MapGet("/", () => "Hello World!");
    app.Run();
  }

  // Example 9: Testing with Let's Encrypt Staging
  public static async Task Example9_Staging() {
    var client = new AcmeClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.Http01,
      Http01Store = new Http01ChallengeStore()
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      "test@example.com",
      "./test-certificates",
      options);

    var domains = new[] { "test.example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "test.example.com");
  }

  // Example 10: Wildcard Certificate with DNS-01
  public static async Task Example10_WildcardCertificate() {
    var client = new AcmeClient();
    var options = new AcmeCertificateManagerOptions {
      PreferredChallengeType = ChallengeType.Dns01,
      Dns01Store = new Dns01ChallengeStore(),
      DnsProvider = new CloudflareDnsProvider(
        apiToken: Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN") ?? "",
        zoneId: Environment.GetEnvironmentVariable("CLOUDFLARE_ZONE_ID") ?? "")
    };

    var manager = new AcmeCertificateManagerV2(
      client,
      "admin@example.com",
      "./certificates",
      options);

    // Only DNS-01 can issue wildcard certificates
    var domains = new[] { "*.example.com", "*.api.example.com", "example.com" };
    var certificate = await manager.ObtainCertificateAsync(domains);
    manager.SaveCertificate(certificate, "wildcard-multi.example.com");
  }
}


