# Native C# ACME Client Implementation

## Overview

Successfully created a comprehensive native C# ACME (Automatic Certificate Management Environment) client for Xiangyao that implements the ACME v2 protocol (RFC 8555) **with support for all three challenge types: HTTP-01, DNS-01, and TLS-ALPN-01**. The implementation uses only BouncyCastle for cryptographic operations and includes multiple DNS provider implementations.

## Project Structure

```
src/Xiangyao.Acme/
├── AcmeClient.cs                    # Core ACME protocol implementation
├── AcmeCertificateManager.cs        # HTTP-01 certificate manager
├── AcmeCertificateManagerV2.cs      # Multi-challenge certificate manager
├── Http01ChallengeStore.cs          # HTTP-01 challenge storage
├── Dns01ChallengeStore.cs           # DNS-01 challenge storage
├── TlsAlpn01ChallengeStore.cs       # TLS-ALPN-01 challenge storage
├── DnsProviders.cs                  # DNS provider implementations
├── AcmeExtensions.cs                # ASP.NET Core integration
├── Examples.cs                      # Usage examples
├── README.md                        # Documentation
└── Xiangyao.Acme.csproj             # Project file
```

## Key Features

### 1. **AcmeClient** - Core ACME Protocol Implementation
- **JWS (JSON Web Signature)**: All requests signed with RS256 algorithm
- **Account Management**: Register new accounts with ACME CA
- **Order Lifecycle**: Create orders, authorize domains, finalize orders
- **All Challenge Types**: HTTP-01, DNS-01, and TLS-ALPN-01 support
- **Certificate Operations**: Download and convert certificates
- **Replay Protection**: Automatic nonce handling

### 2. **Challenge Handlers**
- **HTTP-01**: Thread-safe challenge store with ASP.NET Core middleware
- **DNS-01**: Challenge store + DNS provider abstraction for automated TXT record management
- **TLS-ALPN-01**: Self-signed certificate generation with acmeIdentifier extension

### 3. **Certificate Managers**
- **AcmeCertificateManager**: Simple HTTP-01 only manager
- **AcmeCertificateManagerV2**: Advanced manager with:
  - All three challenge types support
  - Configurable preferred challenge with automatic fallback
  - Wildcard certificate support via DNS-01
  - Automatic challenge setup and cleanup

### 4. **DNS Providers**
- **ManualDnsProvider**: Interactive console-based (for testing)
- **CloudflareDnsProvider**: Full Cloudflare API integration
- **Route53DnsProvider**: Reference implementation for AWS
- **AzureDnsProvider**: Reference implementation for Azure
- **Extensible**: Easy to add custom providers via `IDnsProvider` interface

## Implementation Details

### ACME Protocol Flow

```
1. Initialize Client
   ↓
2. Fetch ACME Directory (endpoints)
   ↓
3. Get Initial Nonce (replay protection)
   ↓
4. Create Account (register with email)
   ↓
5. Create Order (request certificate for domains)
   ↓
6. Get Authorizations (one per domain)
   ↓
7. Complete HTTP-01 Challenges
   ↓
8. Wait for Validation
   ↓
9. Finalize Order (submit CSR)
   ↓
10. Download Certificate
```

### JWS Signing Process

```csharp
// 1. Create Protected Header
{
  "alg": "RS256",
  "nonce": "<replay-nonce>",
  "url": "<request-url>",
  "kid": "<account-url>" or "jwk": {<public-key>}
}

// 2. Encode Payload
payload_base64 = base64url(json_payload)

// 3. Sign
signature = RSA-SHA256(header_base64 + "." + payload_base64)

// 4. Send JWS
{
  "protected": header_base64,
  "payload": payload_base64,
  "signature": signature_base64
}
```

### HTTP-01 Challenge Validation

```
Key Authorization = token + "." + base64url(SHA256(JWK))

Server must respond at:
http://<domain>/.well-known/acme-challenge/<token>

Response body: Key Authorization string
```

## Usage Examples

### HTTP-01 Challenge (Standard)

```csharp
using Xiangyao.Acme;

var client = new AcmeClient();
var challengeStore = new Http01ChallengeStore();
var manager = new AcmeCertificateManager(
    client, 
    challengeStore, 
    "admin@example.com",
    "./certificates");

var domains = new[] { "example.com", "www.example.com" };
var cert = await manager.ObtainCertificateAsync(domains);
manager.SaveCertificate(cert, "example.com");
```

### DNS-01 Challenge (Wildcard Certificates)

```csharp
using Xiangyao.Acme;
using Xiangyao.Acme.DnsProviders;

var client = new AcmeClient();
var options = new AcmeCertificateManagerOptions {
    PreferredChallengeType = ChallengeType.Dns01,
    Dns01Store = new Dns01ChallengeStore(),
    DnsProvider = new CloudflareDnsProvider(
        apiToken: "your-api-token",
        zoneId: "your-zone-id")
};

var manager = new AcmeCertificateManagerV2(
    client,
    "admin@example.com",
    "./certificates",
    options);

// Wildcard support!
var domains = new[] { "*.example.com", "example.com" };
var cert = await manager.ObtainCertificateAsync(domains);
manager.SaveCertificate(cert, "wildcard.example.com");
```

### TLS-ALPN-01 Challenge

```csharp
using Xiangyao.Acme;

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

var cert = await manager.ObtainCertificateAsync(new[] { "example.com" });
```

### ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add all challenge types
builder.Services.AddAcmeChallenges();

var app = builder.Build();

// Enable HTTP-01 challenge middleware
app.UseAcmeHttp01Challenge();
app.MapReverseProxy();

app.Run();
```

### Testing with Staging

```csharp
// Use Let's Encrypt staging for testing
var client = new AcmeClient(
    "https://acme-staging-v02.api.letsencrypt.org/directory");
```

## Security Features

1. **RSA-2048 Keys**: Strong cryptographic keys for account and certificates
2. **Signed Requests**: All ACME requests cryptographically signed
3. **Nonce Protection**: Prevents replay attacks
4. **HTTPS Only**: Production ACME servers use HTTPS
5. **Key Separation**: Account key separate from certificate key

## Comparison with LettuceEncrypt

| Feature | Xiangyao.Acme | LettuceEncrypt |
|---------|---------------|----------------|
| Dependencies | BouncyCastle only | Multiple ACME libs |
| Code Control | Full source control | External dependency |
| Size | ~1400 lines total | Large dependency tree |
| Protocol Support | ACME v2 (RFC 8555) | ACME v2 |
| HTTP-01 | ✅ | ✅ |
| DNS-01 | ✅ | ❌ |
| TLS-ALPN-01 | ✅ | ✅ |
| Wildcard Certs | ✅ (DNS-01) | ❌ |
| DNS Providers | Cloudflare, Manual, Extensible | N/A |
| Customization | Easy to modify | Limited |
| Learning Curve | Lower (see all code) | Higher (black box) |

## Technical Specifications

### Supported ACME Features
- ✅ Directory and nonce retrieval
- ✅ Account creation
- ✅ Order management
- ✅ HTTP-01 challenge
- ✅ DNS-01 challenge
- ✅ TLS-ALPN-01 challenge
- ✅ CSR generation
- ✅ Certificate download
- ✅ JWS signing (RS256)
- ✅ Base64url encoding
- ✅ Wildcard certificates (via DNS-01)
- ❌ Account key rollover (not implemented)
- ❌ Certificate revocation (not implemented)
- ❌ ECC keys (not implemented)

### Challenge Type Comparison

| Feature | HTTP-01 | DNS-01 | TLS-ALPN-01 |
|---------|---------|---------|-------------|
| Wildcard Certs | ❌ | ✅ | ❌ |
| Port Required | 80 | None | 443 |
| Public Access | Required | Not required | Required |
| Setup Complexity | Low | Medium | High |
| DNS Changes | No | Yes | No |
| Firewall Friendly | No | Yes | Partial |

### DNS Provider Support

| Provider | Status | Features |
|----------|--------|----------|
| Manual | ✅ Complete | Interactive console |
| Cloudflare | ✅ Complete | Full API integration |
| AWS Route53 | 📝 Reference | Requires AWS SDK |
| Azure DNS | 📝 Reference | Requires Azure SDK |
| Google Cloud DNS | ❌ Not implemented | - |
| Custom | ✅ Extensible | Implement `IDnsProvider` |

### Cryptographic Operations
- **RSA Key Generation**: 2048-bit keys via BouncyCastle
- **CSR Generation**: PKCS#10 format
- **Signing Algorithm**: SHA256withRSA
- **PFX Conversion**: PKCS#12 format
- **Hash Functions**: 
  - SHA-256 for JWK thumbprint
  - SHA-256 for DNS-01 challenge
  - SHA-256 for TLS-ALPN-01 acmeIdentifier

## Files Created

1. **Xiangyao.Acme.csproj** - Project file targeting net8.0 and net10.0
2. **AcmeClient.cs** - Core ACME protocol client (~350 lines)
3. **AcmeCertificateManager.cs** - HTTP-01 certificate manager (~160 lines)
4. **AcmeCertificateManagerV2.cs** - Multi-challenge certificate manager (~270 lines)
5. **Http01ChallengeStore.cs** - HTTP-01 challenge storage (~30 lines)
6. **Dns01ChallengeStore.cs** - DNS-01 challenge storage (~30 lines)
7. **TlsAlpn01ChallengeStore.cs** - TLS-ALPN-01 challenge storage (~70 lines)
8. **DnsProviders.cs** - DNS provider implementations (~180 lines)
9. **AcmeExtensions.cs** - ASP.NET Core extensions (~50 lines)
10. **Examples.cs** - Usage examples (~200 lines)
11. **README.md** - Comprehensive documentation

## Integration Steps for Xiangyao

To use the native ACME client instead of LettuceEncrypt:

1. **Add Project Reference**:
   ```xml
   <ProjectReference Include="..\Xiangyao.Acme\Xiangyao.Acme.csproj" />
   ```

2. **Configure Services**:
   ```csharp
   builder.Services.AddAcmeHttp01Challenge();
   ```

3. **Add Middleware**:
   ```csharp
   app.UseAcmeHttp01Challenge();
   ```

4. **Create Background Service** for automatic renewal:
   ```csharp
   public class AcmeRenewalService : BackgroundService {
       protected override async Task ExecuteAsync(CancellationToken ct) {
           // Check and renew certificates periodically
       }
   }
   ```

## Future Enhancements

1. **Account Key Persistence**: Save account key to disk for reuse
2. **Automatic Renewal**: Background service with 30-day renewal
3. **Certificate Revocation**: Implement revocation API
4. **ECC Keys**: P-256, P-384 support alongside RSA
5. **External Account Binding**: Support for EAB flow
6. **Rate Limit Handling**: Exponential backoff for retries
7. **More DNS Providers**: Complete AWS Route53, Azure DNS, Google Cloud DNS implementations
8. **OCSP Stapling**: Certificate status checking

## Build Status

✅ Successfully builds with `dotnet build`
✅ Targets net8.0 and net10.0
✅ No breaking changes to existing code
✅ All warnings are from external packages (not our code)
✅ Supports all three ACME challenge types
✅ Wildcard certificate support via DNS-01

## Challenge Type Selection Guide

**Use HTTP-01 when:**
- You need standard single or multi-domain certificates
- Port 80 is accessible
- You don't need wildcard certificates
- Simplest setup is desired

**Use DNS-01 when:**
- You need wildcard certificates (`*.example.com`)
- Domains are behind firewall/NAT
- You have DNS provider API access
- You need to automate many subdomains

**Use TLS-ALPN-01 when:**
- Only port 443 is accessible
- Port 80 is blocked
- DNS changes are not possible
- You have TLS server control

## References

- [RFC 8555 - ACME Protocol](https://tools.ietf.org/html/rfc8555)
- [RFC 7515 - JSON Web Signature](https://tools.ietf.org/html/rfc7515)
- [Let's Encrypt Documentation](https://letsencrypt.org/docs/)
- [BouncyCastle Cryptography](https://www.bouncycastle.org/csharp/)
