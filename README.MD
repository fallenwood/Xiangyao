Xiangyao
---

[![Build Status](https://dev.azure.com/fallenwood/Public%20Projects/_apis/build/status%2FXiangyao%2F%5BCI%5D%20Xiangyao?branchName=master)](https://dev.azure.com/fallenwood/Public%20Projects/_build/latest?definitionId=28&branchName=master) [![Docker Image Size](https://badgen.net/docker/size/fallenwood/xiangyao?icon=docker&label=image%20size)](https://hub.docker.com/r/fallenwood/xiangyao/)

## Introduction
Xiangyao (相繇) is a reverse proxy integrated with YARP, Docker.Net and Xiangyao.Acme.

## Usage

### Docker Provider
See `examples/docker`

### Standalone
See `examples/standalone`

### ACME certificates

Enable HTTPS certificate issuance with Let's Encrypt:

```bash
xiangyao --https --lets-encrypt --acme-email admin@example.com --acme-domain-names example.com
```

Use ZeroSSL instead with an account email. Xiangyao obtains the required EAB credentials from ZeroSSL automatically, matching acme.sh behavior:

```bash
xiangyao --https --zero-ssl --acme-email admin@example.com --acme-domain-names example.com
```

You can also pass dashboard-generated EAB credentials explicitly with `--zero-ssl-eab-kid` and `--zero-ssl-eab-hmac-key`.

### Others
TODO

## Performance
See [benchmarks](./benchmarks/README.MD "")
