# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry manual instrumentation via IConfiguration, supporting both generic-host and standalone apps. Pre-tested example config snippets and configurations for major cloud platforms can be dropped in easily and the underlying OpenTelemetryBuilder stays accessible for adding settings via code.

**Supported Frameworks:** .NET 10.0, .NET 8.0

**Supported .Net App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.

**License:** MIT


| Status | |
| ------ | --- |
| Stability | Alpha |
| Code Owners | [@degero](https://github.com/degero) |


[![NuGet version badge](https://img.shields.io/nuget/v/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![NuGet download count badge](https://img.shields.io/nuget/dt/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![codecov](https://codecov.io/gh/degero/simpleopentelemetry/graph/badge.svg?token=USK6CSKHSJ)](https://codecov.io/gh/degero/simpleopentelemetry)


## Goal

*To make OpenTelemetry manual instrumentation as convenient as possible so developers can focus on their apps*


## Overview


SimpleOpenTelemetry handles configuration when using manual code-based OpenTelemetry setup. Rather than developers writing code in their app using OpenTelemetry's fluent api, settings are defined in a configuration file / env vars and it calls fluent api code based on this. It is not in any way related to [auto-instrumentation/zero-code](https://opentelemetry.io/docs/concepts/instrumentation/zero-code/) and is designed to streamline setup for most common configurations. If you need to extend on what SimpleOpenTelemetry provides, you can access the OpenTelemetryBuilder to run any of OpenTelemetry's fluent api methods.


## Features

- Pluggable components by adding config entry and NuGet package to your app for telemetry features you need
- Pre-tested example configuration files for common app / cloud platform / 3rd party telemetry service scenarios [docs/configuration/examples](./docs/configuration/examples/)
- Component snippets so you can quickly add in extra otel components [docs/configuration/snippets](./docs/configuration/snippets/)
- Cloud examples for AWS, Azure and GCP in [example-apps/cloud/](./example-apps/cloud/)
- Key Configuration validation via `SimpleOpenTelemetryValidate()` extension method
- Ability to register multiple exporters with different configurations easily
- Set telemetry attribute 'service.version' based on app assembly version when using builtin SimpleOpenTelemetry ResourceDetector 'AssemblyVersion' (see [AssemblyVersion](#assemblyversion)). Overridden by setting 'service.version' in OTEL_RESOURCE_ATTRIBUTES of appsettings.json / env var
- 'All signal' exporter options overridable at the signal level for exporter type
- `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` packages are included in this lib. Making Generic host registration and OTLP export.


## Limitations

- Complex types or Action<>/Func<>/etc on properties of component options (eg Instrumentation, exporters etc) are not supported which may limit your ability to control some telemetry (eg. AspNetCoreInstrumentation sending GET /health telemetry). These can components with complex options can still be set via code if needed.
- Not all of [opentelemetry-dotnet-contrib](https://github.com/open-telemetry/opentelemetry-dotnet-contrib) components are supported. You can use SimpleOpenTelemetry and add any via code or raise a PR / [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new) to have it added.


## Quickstart

Run the aspnetcore example app guide in [example-apps/localdev/README.md](./example-apps/localdev/README.md) for local SimpleOpenTelemetry with Grafana LGTM running in docker to view telemetry. This can be used as a good starting point to test out building a config to your needs or for apps setup ready to deploy to the cloud use [example-apps/cloud/](./example-apps/cloud/)


## Documentation

Documentation for setting up SimpleOpenTelemetry can be found in [docs/README.md](./docs/README.md)


## License

MIT License - see [LICENSE](./LICENSE) file for details.


## Contribution

Issues, feature requests, or contributions are most welcome.
