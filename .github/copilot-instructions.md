# Copilot Instructions for Xiangyao

## Project Overview

Xiangyao (相繇) is a C# ASP.NET Core reverse proxy that integrates with YARP (Yet Another Reverse Proxy), Docker.NET, and LettuceEncrypt. The project reads Docker API to discover living backends and uses container labels to build dynamic routing tables.

## Development Workflow

### Build Command
```bash
dotnet build
```

### Required Before Each Commit
- Ensure code builds successfully with `dotnet build`
- Run tests if making changes to core functionality
- Follow the established code formatting and style conventions

## Repository Structure

- **`src/`** - Main source code
  - `Xiangyao/` - Core reverse proxy application
  - `Xiangyao.DockerClient/` - Docker API client integration
  - `Xiangyao.Portal/` - Admin interface components
  - `Xiangyao.Telemetry/` - OpenTelemetry integration
  - `Xiangyao.Common/` - Shared utilities and models
- **`tests/`** - Unit and integration tests
- **`examples/`** - Demonstration projects showing usage
- **`admin/`** - Admin interface components
- **`thirdparty/`** - Fork of third-party packages
- **`benchmarks/`** - Performance benchmarks for utility methods

## Code Standards and Guidelines

### C# Best Practices
1. **Respect existing code format style** - Follow the patterns already established in the codebase
2. **Use latest C# and .NET features** - Leverage modern C# idioms and patterns when appropriate
3. **Maintain existing code structure** - Preserve the established project organization and architecture
4. **Write unit tests** - Add tests for new functionality, preferably using table-driven test patterns when applicable

### Key Architecture Concepts
- **Docker Integration**: The application monitors Docker containers and uses labels to configure routing
- **YARP Integration**: Leverages Microsoft's YARP for reverse proxy functionality
- **Label Parsing**: Container labels follow patterns like `xiangyao.routes.{name}.match.host` for configuration
  ```yaml
  labels:
    - "xiangyao.enable=true"
    - "xiangyao.cluster.port=80"
    - "xiangyao.cluster.schema=http"
    - "xiangyao.routes.nginx_http.match.host=localhost:5000"
    - "xiangyao.routes.nginx_http.match.path={**catch-all}"
  ```
- **Provider Pattern**: Uses different providers (Docker, File) for configuration sources
- **Hosted Services**: Background services monitor and update configurations dynamically

### Common Patterns in Codebase
- **Dependency Injection**: Heavy use of ASP.NET Core DI container
- **Configuration Providers**: Implement `IProxyConfigProvider` for different configuration sources
- **Label Parsing**: Use `ILabelParser` implementations for parsing Docker container labels
- **Telemetry**: OpenTelemetry integration for monitoring and observability

### Testing Guidelines
- Use **xUnit** testing framework with **FluentAssertions** for assertions
- Prefer table-driven unit tests using `[Theory]` and `[InlineData]` when testing multiple scenarios
- Mock external dependencies (Docker API, file system, etc.)
- Test both success and failure scenarios
- Follow existing test patterns in the `tests/` directory
- Example test pattern:
  ```csharp
  [Theory]
  [InlineData(typeof(StateMachineLabelParser))]
  [InlineData(typeof(SwitchCaseLabelParser))]
  public void Test_Parse_Labels(Type parserType) {
    // Test implementation using FluentAssertions
    result.Should().NotBeNull();
    result.Should().Be(expectedValue);
  }
  ```

## Key Components to Understand

### Docker Provider (`src/Xiangyao/Docker/`)
- `DockerProxyConfigProvider` - Main configuration provider for Docker-based routing
- `ILabelParser` implementations - Parse container labels into routing configuration
- `DockerSocket` - Low-level Docker API communication

### Core Application (`src/Xiangyao/Program.cs`)
- Entry point with command-line argument parsing
- Service registration and configuration
- Support for different providers (Docker, File, etc.)

### Portal (`src/Xiangyao.Portal/`)
- Web-based admin interface for viewing current configuration
- API endpoints for configuration introspection

## Development Tips
- The application uses modern .NET patterns with minimal APIs and dependency injection
- Container labels drive the routing configuration - understand the label parsing logic
- The application supports multiple configuration providers - Docker is the primary one
- OpenTelemetry is integrated for observability - leverage existing telemetry when adding features