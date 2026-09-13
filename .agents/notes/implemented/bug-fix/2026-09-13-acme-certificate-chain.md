# Agent Note: ACME certificate chain preservation

Status: implemented

English | [中文](2026-09-13-acme-certificate-chain.zh.md)

## Problem

When the ACME server returns a certificate bundle, Xiangyao previously stored only the first certificate inside the generated PFX. Intermediate certificates were dropped, leaving the PKCS#12 archive without the full chain required for browsers and TLS clients to validate the certificate correctly.

## Decision

The ACME certificate conversion now writes every certificate returned in the PEM bundle into the PKCS#12 store, attaches the private key to the full chain, and preserves the leaf certificate together with all intermediates in the same archive. The key entry remains associated with the complete certificate chain so clients can build the trust path directly from the exported PFX.

## Alternatives considered

- Keep only the leaf certificate: smallest possible patch, but it breaks chain validation for browsers and TLS clients.
- Split the certificate chain into separate files and require external orchestration: preserves the signer chain but adds operational complexity and configuration drift.
- Re-export only the first certificate again: rejects the issue without fixing the underlying chain loss.

## Consequences

- TLS clients can validate certificates against the full issuer chain shipped in the PFX.
- Renewed certificates remain usable without manual chain reconstruction.
- The exported archive is slightly larger, but it reflects the complete certificate bundle returned by the ACME authority.