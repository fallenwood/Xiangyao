# Xiangyao.Acme

A native C# ACME (Automatic Certificate Management Environment) client implementation for Xiangyao proxy that implements the ACME v2 protocol (RFC 8555) without third-party dependencies (except BouncyCastle for cryptographic operations).

## Features

- **Pure C# Implementation**: Implements ACME v2 protocol using only native .NET libraries
- **Multiple Challenge Types**: 
  - **HTTP-01**: Standard web-based validation
  - **DNS-01**: DNS TXT record validation (supports wildcard certificates)
  - **TLS-ALPN-01**: TLS with ALPN validation
- **Automatic Certificate Management**: Obtains and renews SSL/TLS certificates automatically
- **Let's Encrypt Compatible**: Works with Let's Encrypt and other ACME-compliant CAs
- **Minimal Dependencies**: Only uses BouncyCastle for cryptographic operations
- **Wildcard Certificates**: Full support via DNS-01 challenge
- **Extensible DNS Providers**: Built-in support for Cloudflare, manual setup, and extensible for AWS Route53, Azure DNS, etc.

## Architecture

### Core Components

1. **AcmeClient**: Main client for ACME protocol communication
   - JWS (JSON Web Signature) signing
   - Account creation and management
   - Order lifecycle management
   - Challenge completion (HTTP-01, DNS-01, TLS-ALPN-01)
   - CSR generation and submission
   - Certificate download

2. **Challenge Stores**:
   - **Http01ChallengeStore**: In-memory store for HTTP-01 challenges
   - **Dns01ChallengeStore**: In-memory store for DNS-01 challenges
   - **TlsAlpn01ChallengeStore**: Certificate store for TLS-ALPN-01 challenges

3. **AcmeCertificateManager / AcmeCertificateManagerV2**: High-level certificate management
   - End-to-end certificate acquisition workflow
   - Automatic challenge handling
   - PFX and PEM export
   - Multi-challenge support with fallback

4. **DNS Providers**:
   - **ManualDnsProvider**: Interactive console-based DNS setup (for testing)
   - **CloudflareDnsProvider**: Automatic Cloudflare DNS integration
   - **Route53DnsProvider**: AWS Route53 (reference implementation)
   - **AzureDnsProvider**: Azure DNS (reference implementation)

## Usage

### HTTP-01 Challenge (Standard)

Best for: Single domain certificates where you control the web server

```csharp
using Xiangyao.Acme;

// Create ACME client
var client = new AcmeClient(); // Uses Let's Encrypt production by default

// Create certificate manager
var challengeStore = new Http01ChallengeStore();
var manager = new AcmeCertificateManager(
    client,
    challengeStore,
    "your-email@example.com",
    "./certificates");

// Obtain certificate
var domains = new[] { "example.com", "www.example.com" };
var certificate = await manager.ObtainCertificateAsync(domains);

// Save certificate
manager.SaveCertificate(certificate, "example.com");
```

### DNS-01 Challenge (Wildcard Support)

Best for: Wildcard certificates, domains behind firewall, multiple subdomains

```csharp
using Xiangyao.Acme;
using Xiangyao.Acme.DnsProviders;

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
    "your-email@example.com",
    "./certificates",
    options);

// DNS-01 supports wildcard certificates!
var domains = new[] { "*.example.com", "example.com" };
var certificate = await manager.ObtainCertificateAsync(domains);
manager.SaveCertificate(certificate, "wildcard.example.com");
```

### TLS-ALPN-01 Challenge

Best for: Systems where HTTP and DNS are not accessible

```csharp
using Xiangyao.Acme;

var client = new AcmeClient();
var options = new AcmeCertificateManagerOptions {
    PreferredChallengeType = ChallengeType.TlsAlpn01,
    TlsAlpn01Store = new TlsAlpn01ChallengeStore()
};

var manager = new AcmeCertificateManagerV2(
    client,
    "your-email@example.com",
    "./certificates",
    options);

var domains = new[] { "example.com" };
var certificate = await manager.ObtainCertificateAsync(domains);
manager.SaveCertificate(certificate, "example.com");
```

### ASP.NET Core Integration

```csharp
using Xiangyao.Acme;

// Add services
builder.Services.AddAcmeChallenges(); // Adds all challenge types

var app = builder.Build();

// Add ACME HTTP-01 challenge middleware (must be before routing)
app.UseAcmeHttp01Challenge();
app.MapReverseProxy();
```

### Testing with Let's Encrypt Staging

```csharp
// Use staging environment for testing
var client = new AcmeClient("https://acme-staging-v02.api.letsencrypt.org/directory");
```

## ACME Protocol Flow

1. **Initialize**: Fetch ACME directory and get initial nonce
2. **Create Account**: Register account with email address
3. **Create Order**: Request certificate for domain names
4. **Get Authorizations**: Retrieve challenges for each domain
5. **Complete Challenges**: Set up HTTP-01 challenge responses
6. **Wait for Validation**: Poll until challenges are validated
7. **Finalize Order**: Submit CSR (Certificate Signing Request)
8. **Download Certificate**: Retrieve issued certificate

## Challenge Types Explained

### HTTP-01 Challenge

**How it works**: ACME server makes HTTP request to `http://<domain>/.well-known/acme-challenge/<token>` and expects the key authorization as response.

**Pros**:
- Simple to implement
- No DNS changes required
- Works with standard web servers

**Cons**:
- Requires port 80 accessible
- Cannot issue wildcard certificates
- Domain must be publicly accessible

**Use case**: Standard single or multi-domain certificates

### DNS-01 Challenge

**How it works**: Create a TXT record at `_acme-challenge.<domain>` with the computed value, ACME server queries DNS.

**Pros**:
- Supports wildcard certificates (`*.example.com`)
- Works for domains behind firewall
- No need for port 80/443 access

**Cons**:
- Requires DNS provider API access
- DNS propagation delay (~30 seconds)
- More complex setup

**Use case**: Wildcard certificates, internal domains, multiple subdomains

### TLS-ALPN-01 Challenge

**How it works**: ACME server connects on port 443 with ALPN protocol `acme-tls/1` and validates a special certificate with acmeIdentifier extension.

**Pros**:
- Works on port 443 only
- No HTTP server needed
- No DNS changes required

**Cons**:
- Requires TLS server control
- Cannot issue wildcard certificates
- More complex implementation

**Use case**: Environments where only TLS traffic is allowed

## Certificate Storage

Certificates are saved in two formats:
- **PFX** (.pfx): Binary format with private key (used by Kestrel)
- **PEM** (.pem): Text format certificate chain

## Security Considerations

1. **Account Key**: Automatically generated RSA-2048 key pair per client instance
2. **Certificate Key**: Fresh RSA-2048 key pair generated for each certificate
3. **JWS Signing**: All ACME requests are signed using RS256 algorithm
4. **Replay Protection**: Nonce values prevent replay attacks

## Integration with Xiangyao

This ACME client can replace the `LettuceEncrypt` dependency in Xiangyao proxy. To integrate:

1. Reference `Xiangyao.Acme` project
2. Add ACME services and middleware
3. Create hosted service for automatic certificate renewal
4. Configure Kestrel to use obtained certificates

## Dependencies

- **BouncyCastle.Cryptography**: RSA key generation, CSR creation, PFX conversion
- **Microsoft.AspNetCore.App**: HTTP middleware for challenge responses

## Limitations

- Account key not persisted (new account created each time)
- Certificate renewal must be triggered manually or via custom scheduler
- TLS-ALPN-01 requires advanced Kestrel configuration
- DNS provider implementations for AWS/Azure are reference only (require respective SDKs)

## Future Enhancements

- [x] DNS-01 challenge support for wildcard certificates
- [x] TLS-ALPN-01 challenge support
- [x] Multiple DNS provider implementations
- [ ] Persistent account key storage
- [ ] Automatic certificate renewal with background service
- [ ] Certificate revocation support
- [ ] ECC key support (P-256, P-384)
- [ ] External account binding (EAB) support
- [ ] More DNS provider implementations (AWS Route53, Azure DNS, Google Cloud DNS)

## References

- [RFC 8555 - ACME Protocol](https://tools.ietf.org/html/rfc8555)
- [Let's Encrypt ACME Documentation](https://letsencrypt.org/docs/client-options/)
- [JWS Specification (RFC 7515)](https://tools.ietf.org/html/rfc7515)
