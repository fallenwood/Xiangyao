Xiangyao
---

[![Build Status](https://dev.azure.com/fallenwood/Public%20Projects/_apis/build/status%2FXiangyao%2F%5BCI%5D%20Xiangyao?branchName=master)](https://dev.azure.com/fallenwood/Public%20Projects/_build/latest?definitionId=28&branchName=master) [![Docker Image Size](https://badgen.net/docker/size/fallenwood/xiangyao?icon=docker&label=image%20size)](https://hub.docker.com/r/fallenwood/xiangyao/)

## Introduction
Xiangyao (相繇) is a reverse proxy integrated with YARP, Docker.Net and native ACME (Let's Encrypt) support.

## Features
- **YARP Integration**: Full reverse proxy functionality with YARP
- **Docker Provider**: Automatic route discovery from Docker container labels
- **Native ACME Client**: Built-in Let's Encrypt support with HTTP-01, DNS-01, and TLS-ALPN-01 challenges
- **Wildcard Certificates**: Support for wildcard certificates via DNS-01 challenge
- **OpenTelemetry**: Built-in observability with traces, metrics, and logs

## Usage

### Docker Provider
See `examples/docker`

### Standalone
See `examples/standalone`

### Others
TODO

## Performance
See [benchmarks](./benchmarks/README.MD "")
