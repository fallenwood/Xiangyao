# Agent Note: Xiangyao 反向代理架构

Status: implemented

[English](2026-09-13-xiangyao-architecture.md) | 中文

## Problem

Xiangyao 必须为容器化服务提供动态 HTTPS 路由，而不要求运维人员维护静态的 NGINX 或 Envoy 配置。它还需要自动证书签发，以及一个统一入口来观察 Docker 生命周期事件和路由状态。

## Decision

Xiangyao 采用 .NET 反向代理服务实现，核心依赖 YARP、Docker.Net 和原生 ACME 客户端。Docker 标签定义路由元数据，容器变化时会刷新代理配置，Portal 也暴露同一套运行时状态。证书由 Let's Encrypt 或 ZeroSSL 直接签发与续期，而不是依赖独立的证书管理器进程。

## Alternatives considered

- 仅使用静态反向代理配置：部署更简单，但无法可靠跟踪容器生命周期和按服务的路由更新。
- 依赖外部证书工具：在边缘环境中更容易隔离，但会重复续期逻辑，并让代理与证书生命周期出现漂移。
- 仅依赖 Docker 运行而不提供 Portal：组件更少，但在验证、排障和运行时可观测性方面明显不足。

## Consequences

- 路由生成始终与当前 Docker 状态保持一致。
- HTTPS 和通配符证书的申请与续期直接集成进运行时。
- 运行时通过将反向代理、Docker 集成和 ACME 合并为一个可执行文件，保持了较小的操作面。
- Portal 和遥测能力被视为一等运维能力，而不是可选增强项。
