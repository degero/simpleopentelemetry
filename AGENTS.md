# AGENTS instructions for SimpleOpenTelemetry

## Repository purpose

- This repository ships the SimpleOpenTelemetry NuGet package from [src/SimpleOpenTelemetry](src/SimpleOpenTelemetry).
- The package is intentionally configuration-driven: most behavior is enabled through IConfiguration rather than hand-written OpenTelemetry setup code.

## Working conventions

- Prefer changes that preserve the library's low-code, configuration-first design.
- Keep public API changes minimal and update [src/SimpleOpenTelemetry/README.nuget.md](src/SimpleOpenTelemetry/README.nuget.md) when usage or packaging guidance changes.
- When changing configuration shape, supported components, or bootstrap behavior, update the relevant docs under [docs](docs) and any matching example app in [example-apps](example-apps).
- Add or update tests in [tests/SimpleOpenTelemetry.Tests](tests/SimpleOpenTelemetry.Tests) and [tests/SimpleOpenTelemetry.IntegrationTests](tests/SimpleOpenTelemetry.IntegrationTests) for behavior changes.

## Build and test

- Build the solution with `dotnet build`.
- Run the test suite with `dotnet test SimpleOpenTelemetry.sln`.

## Key package files

- [src/SimpleOpenTelemetry/SimpleOpenTelemetryBootstrap.cs](src/SimpleOpenTelemetry/SimpleOpenTelemetryBootstrap.cs): standalone bootstrap entry point for non-generic-host apps.
- [src/SimpleOpenTelemetry/Extensions/HostApplicationBuilderExtensions.cs](src/SimpleOpenTelemetry/Extensions/HostApplicationBuilderExtensions.cs): generic-host registration entry point.
- [src/SimpleOpenTelemetry/Builder/SimpleOpenTelemetryBuilder.cs](src/SimpleOpenTelemetry/Builder/SimpleOpenTelemetryBuilder.cs): core configuration processing and component wiring.
- [src/SimpleOpenTelemetry/SimpleOpenTelemetryOptions.cs](src/SimpleOpenTelemetry/SimpleOpenTelemetryOptions.cs): configuration model used by the package.
- [src/SimpleOpenTelemetry/README.nuget.md](src/SimpleOpenTelemetry/README.nuget.md): package-facing README used for NuGet packaging.

## Useful starting points

- Start with [README.md](README.md) for repo-level context.
- Use [docs/README.md](docs/README.md) for configuration and component reference.
- Review [example-apps/localdev](example-apps/localdev) for a practical end-to-end setup.
