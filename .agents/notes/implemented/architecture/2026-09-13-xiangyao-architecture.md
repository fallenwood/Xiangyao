# Agent Note: Xiangyao reverse proxy architecture

Status: implemented

English | [中文](2026-09-13-xiangyao-architecture.zh.md)

## Problem

Xiangyao must provide dynamic HTTPS routing for containerized services without requiring operators to maintain static upstream configuration. It also needs automatic certificate issuance and a single place to observe Docker lifecycle events and route health.

## Decision

Xiangyao is implemented as a .NET reverse-proxy service built around YARP, Docker.Net, and a native ACME client. Docker labels define route metadata, the proxy config is refreshed when containers change, and the portal shares the same operational state as the runtime. Certificates are provisioned and renewed directly through Let's Encrypt or ZeroSSL instead of depending on a separate certificate manager process.

## Alternatives considered

- Static reverse-proxy configuration only: simpler to operate, but it does not track container churn or per-service route changes reliably.
- External certificate tooling: easier to isolate at the edge, but it duplicates renewal logic and increases drift between the proxy and the certificate lifecycle.
- Docker-only operation without a portal: smaller in footprint, but weaker for verification, troubleshooting, and runtime visibility.

## Consequences

- Route generation stays aligned with the current Docker state.
- HTTPS and wildcard certificate provisioning is integrated directly into the runtime.
- The runtime keeps a compact operational surface by combining reverse proxying, Docker integration, and ACME in one executable.
- The portal and telemetry surfaces are treated as first-class operational concerns instead of optional addons.
