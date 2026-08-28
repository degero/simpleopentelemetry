# SimpleOpenTelemetry

[![NuGet version badge](https://img.shields.io/nuget/v/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![NuGet download count badge](https://img.shields.io/nuget/dt/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![CI](https://github.com/degero/SimpleOpenTelemetry/actions/workflows/main.yml/badge.svg)](https://github.com/degero/SimpleOpenTelemetry/actions/workflows/main.yml)
[![codecov](https://codecov.io/gh/degero/simpleopentelemetry/graph/badge.svg?token=USK6CSKHSJ)](https://codecov.io/gh/degero/simpleopentelemetry)
[![CodeQL](https://github.com/degero/SimpleOpenTelemetry/actions/workflows/codeql.yml/badge.svg)](https://github.com/degero/SimpleOpenTelemetry/actions/workflows/codeql.yml)
[![Lint](https://github.com/degero/SimpleOpenTelemetry/actions/workflows/lint.yml/badge.svg)](https://github.com/degero/SimpleOpenTelemetry/actions/workflows/lint.yml)

A lightweight, low-code .NET library for configuring OpenTelemetry code-based instrumentation via IConfiguration, supporting both generic-host and standalone apps. Pre-tested example configurations for major cloud platforms can be dropped in easily to get telemetry flowing quickly. The underlying OpenTelemetryBuilder stays accessible for adding settings via code.

[NuGet Package](https://www.nuget.org/packages/SimpleOpenTelemetry)

**Supported .Net Versions:** .NET 10.0, .NET 8.0, .NET Standard 2.0

**Supported .Net App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.

| Status      |                                      |
| ----------- | ------------------------------------ |
| Stability   | Beta                                 |
| Code Owners | [@degero](https://github.com/degero) |

| SimpleOpenTelemetry | OpenTelemetry SDK family |
| ------------------- | ------------------------ |
| 0.1.x               | 1.16.x                   |

<details>
<summary>Table of Contents</summary>

- [Goal](#goal)
- [Overview](#overview)
- [Features](#features)
- [Limitations](#limitations)
- [Quickstart](#quickstart)
- [Documentation](#documentation)
- [Releases](#releases)
- [Supported OpenTelemetry components](#supported-opentelemetry-components)
- [Dependencies](#dependencies)
- [Support the project](#support-the-project)
- [Feedback](#feedback)
- [Contributing](#contributing)
- [License](#license)

</details>

## Goal

_To make OpenTelemetry instrumentation simple so developers can focus on their apps, not observability setup_

## Overview

SimpleOpenTelemetry allows configuration via IConfiguration when using [code-based instrumentation](https://opentelemetry.io/docs/concepts/instrumentation/code-based/). Rather than adding (sometimes quite a lot of) code to your app to setup collection and exporting of telemetry, settings defined in configuration are processed by SimpleOpenTelemetry and the fluent api is invoked. It is designed to streamline setup for most common configurations with major cloud providers and general OTLP export with included example configurations.

If you need to extend on what SimpleOpenTelemetry provides, you can access the OpenTelemetryBuilder to run any of OpenTelemetry's fluent api methods after the configuration has processed. The use of OpenTelemetry here is not related to [auto-instrumentation/zero-code instrumentation](https://opentelemetry.io/docs/concepts/instrumentation/zero-code/)

## Features

- One line OpenTelemetry initialisation via `builder.AddSimpleOpenTelemetry()` or for non-generic host applications: `SimpleOpenTelemetryBootstrap.Add()`.
- Plug in [supported OpenTelemetry components](#supported-opentelemetry-components) by adding a config entry and NuGet package to your app for telemetry features you need (eg Exporters, Instrumentation, Resource detectors etc)
- Dependency to OpenTelemetry.Exporter.OpenTelemetryProtocol included for convenience
- Ability to use other [IConfiguration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) other than file and environment variables (eg for secrets)
- Pre-tested example configuration files for common app / cloud platform / 3rd party telemetry service scenarios [docs/configuration/examples](./docs/configuration/examples/)
- Component snippets so you can quickly add in extra otel components [docs/configuration/snippets](./docs/configuration/snippets/)
- Cloud example apps for AWS, Azure and GCP in [example-apps/cloud/](./example-apps/cloud/)
- Local development example apps for testing and fine-tuning your telemetry collection setup and viewing telemetry in Grafana [example-apps/localdev](./example-apps/localdev/README.md)
- Ability to register multiple exporters with different configurations easily
- 'All signal' exporter options overridable at the signal level for exporter type
- Built-in SimpleOpenTelemetry [AssemblyVersion](docs/configuration/resource-detectors.md#assemblyversion) ResourceDetector. Enabled by configuration, this sets telemetry attribute 'service.version' based on app assembly version.
- Essential Otel Resource Attribute / Service name validation via `SimpleOpenTelemetryValidate()` extension method [SimpleOpenTelemetryValidator.cs](./src//SimpleOpenTelemetry/Validation/SimpleOpenTelemetryValidator.cs)

## Limitations

- Not all of [opentelemetry-dotnet-contrib](https://github.com/open-telemetry/opentelemetry-dotnet-contrib) components are supported see [supported opetelemetry components](#supported-opentelemetry-components). You can use SimpleOpenTelemetry and add any unsupported ones via code or raise a PR / [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new) to have it added.
- Cloud services regarding OpenTelemetry are rapidly evolving. The provided example configurations may become outdated if their hosted OpenTelemetry services change. Ensure you verify your configuration before deploying.
- Complex types or Action<>/Func<>/etc on properties of component fluentapi registration options (eg Instrumentation, exporters etc) are not supported which may limit your ability to control some telemetry (eg. AspNetCoreTraceInstrumentationOptions.Filter). Components with complex options can still be set via code if needed.

## Quickstart

Run the [localdev aspnetcore example app](./example-apps/localdev/README.md) in this repo or use the [Nuget package Quickstart guide](https://www.nuget.org/packages/SimpleOpenTelemetry#quickstart). Both result in a local aspnetcore mvc app using SimpleOpenTelemetry with Grafana LGTM running in docker to view telemetry.

These can be used as a good starting point to test out building a config to your needs using the provided [config documentation](/docs//configuration/README.md), [examples](docs/configuration/examples/) and [snippets](docs/configuration/snippets/README.md). For apps setup ready to deploy to the cloud use [example-apps/cloud/](./example-apps/cloud/)

## Documentation

[docs/README.md](./docs/README.md)

## Releases

[Releases](http://github.com/degero/simpleopentelemetry/releases)

[CHANGELOG.md](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/CHANGELOG.md)

## Supported OpenTelemetry components

OpenTelemetry, OpenTelemetry-contrib and other 3rd parties have many otel components published as NuGet packages. For a list of OpenTelemetry packages you can use with SimpleOpenTelemetry see [SimpleOpenTelemetry supported otel components](./docs/otel-component-versions.md). ⚠️ **It is recommended you install these versions of component packages.** ⚠️

It is still possible to add other components not listed here, but only via the code-based OpenTelemetry fluent api.

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

## Support the project

If you find SimpleOpenTelemetry helpful please kindly consider buying me a ☕ via the 💗 Sponsor button at the top of repo page or [ko-fi.com/degero](https://ko-fi.com/degero) 🙏

## Feedback

For issues, feature requests etc please submit here: [SimpleOpenTelemetry issues](https://github.com/degero/simpleopentelemetry/issues/new)

## Contributing

Contributions are most welcome.
[CONTRIBUTING](./CONTRIBUTING.md)
[MAINTAINING](./MAINTAINING.md)

## License

MIT License - see [LICENSE](./LICENSE) file for details.
