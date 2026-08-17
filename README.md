# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry code-based instrumentation via IConfiguration, supporting both generic-host and standalone apps. Pre-tested example config snippets and configurations for major cloud platforms can be dropped in easily and the underlying OpenTelemetryBuilder stays accessible for adding settings via code.

**Supported Frameworks:** .NET 10.0, .NET 8.0, .NET Standard 2.0

**Supported .Net App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.

**License:** MIT

| Status      |                                      |
| ----------- | ------------------------------------ |
| Stability   | Beta                                 |
| Code Owners | [@degero](https://github.com/degero) |

[![NuGet version badge](https://img.shields.io/nuget/v/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![NuGet download count badge](https://img.shields.io/nuget/dt/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![codecov](https://codecov.io/gh/degero/simpleopentelemetry/graph/badge.svg?token=USK6CSKHSJ)](https://codecov.io/gh/degero/simpleopentelemetry)

[CHANGELOG.md](./CHANGELOG.md)

## Dependencies

| Package                                                                                                                     | Version  | Notes                         |
| --------------------------------------------------------------------------------------------------------------------------- | -------- | ----------------------------- |
| [OpenTelemetry](https://www.nuget.org/packages/OpenTelemetry)                                                               | `1.16.0` | Core SDK                      |
| [OpenTelemetry.Extensions.Hosting](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting)                         | `1.16.0` | IHostBuilder / DI integration |
| [OpenTelemetry.Exporter.OpenTelemetryProtocol](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol) | `1.16.0` | OTLP exporter                 |

These versions are **PINNED** if your project already references these packages at a different version, NuGet restore will fail (`NU1608`/`NU1107`) or emit a version-conflict warning as . To resolve this, either:

- Remove your direct `PackageReference` entries for these three packages and let `SimpleOpenTelemetry` supply them, or
- Downgrade/align your direct references to match the pinned versions.

NOTE: There are also Microsoft.\* are transitive deps from OpenTelemetry SDK Family.

## Compatibility

| SimpleOpenTelemetry | OpenTelemetry SDK family |
| ------------------- | ------------------------ |
| 0.1.x               | 1.16.x                   |

## Goal

_To make OpenTelemetry code-based instrumentation as simple as possible so developers can focus on their apps_

## Overview

SimpleOpenTelemetry handles configuration via IConfiguration rather than code calling OpenTelemetry's fluent api when using code-based instrumentation. Settings defined in configuration are processed by SimpleOpenTelemetry and the fluent api is invoked. It is designed to streamline setup for most common configurations. If you need to extend on what SimpleOpenTelemetry provides, you can access the OpenTelemetryBuilder to run any of OpenTelemetry's fluent api methods. The use of OpenTelemetry here is not related to [auto-instrumentation/zero-code instrumenation](https://opentelemetry.io/docs/concepts/instrumentation/zero-code/)

## Features

- One line OpenTelemetry initialisation via `builder.AddSimpleOpenTelemetry()` or `SimpleOpenTelemetryBootstrap.Add()` for non-generic host applications.
- Plug in OpenTelemetry components by adding a config entry and NuGet package to your app for telemetry features you need (eg Instrumentation, Resource detectors etc)
- Pre-tested example configuration files for common app / cloud platform / 3rd party telemetry service scenarios [docs/configuration/examples](./docs/configuration/examples/)
- Component snippets so you can quickly add in extra otel components [docs/configuration/snippets](./docs/configuration/snippets/)
- Cloud example apps for AWS, Azure and GCP in [example-apps/cloud/](./example-apps/cloud/)
- Local development example apps for testing and fine-tuning your telemetry collection setup and viewing telemetry in Grafana [example-apps/localdev](./example-apps/localdev/README.md)
- Ability to register multiple exporters with different configurations easily
- 'All signal' exporter options overridable at the signal level for exporter type
- Set telemetry attribute 'service.version' based on app assembly version when using builtin SimpleOpenTelemetry ResourceDetector 'AssemblyVersion' (see [AssemblyVersion](#assemblyversion)). Overridden by setting 'service.version' in OTEL_RESOURCE_ATTRIBUTES of appsettings.json / env var
- Essential Otel Resource Attribute / Service name validation via `SimpleOpenTelemetryValidate()` extension method [SimpleOpenTelemetryValidator.cs](./src//SimpleOpenTelemetry/Validation/SimpleOpenTelemetryValidator.cs)

## Limitations

- Complex types or Action<>/Func<>/etc on properties of component options (eg Instrumentation, exporters etc) are not supported which may limit your ability to control some telemetry (eg. AspNetCoreInstrumentation sending GET /health telemetry). These can components with complex options can still be set via code if needed.
- Not all of [opentelemetry-dotnet-contrib](https://github.com/open-telemetry/opentelemetry-dotnet-contrib) components are supported. You can use SimpleOpenTelemetry and add any via code or raise a PR / [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new) to have it added.

## Supported OpenTelemetry components

OpenTelemetry, OpenTelemetry-contrib and other 3rd parties have many otel components published as NuGet packages. For a list of supported / unit tested OpenTelemetry packages you can plug in see [SimpleOpenTelemetry tested otel components](./docs/otel-component-versions.md).

⚠️ **Ensure you install these versions of component packages.** ⚠️

## Quickstart

Run the aspnetcore example app guide in [example-apps/localdev/README.md](./example-apps/localdev/README.md) for local SimpleOpenTelemetry with Grafana LGTM running in docker to view telemetry. This can be used as a good starting point to test out building a config to your needs or for apps setup ready to deploy to the cloud use [example-apps/cloud/](./example-apps/cloud/)

## Documentation

Documentation for setting up SimpleOpenTelemetry can be found in [docs/README.md](./docs/README.md)

## License

MIT License - see [LICENSE](./LICENSE) file for details.

## Feedback

For issues, feature requests etc please submit here: [SimpleOpenTelemetry issues](https://github.com/degero/simpleopentelemetry/issues/new)

## Contributing

Contributions are most welcome.

[CONTRIBUTING](https://github.com/degero/simpleopentelemetry/blob/main/CONTRIBUTING.md)
