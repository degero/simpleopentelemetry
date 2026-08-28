# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry code-based instrumentation via IConfiguration, supporting both generic-host and standalone apps. Pre-tested example configurations for major cloud platforms can be dropped in easily to get telemetry flowing quickly. The underlying OpenTelemetryBuilder stays accessible for adding settings via code. Check out the [Features](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/README.md#features) and [Limitations](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/README.md#limitations) before diving in to be sure SimpleOpenTelemetry covers your needs.

**Supported .NET Versions:** .NET 10.0, .NET 8.0, .NET Standard 2.0

**Supported .NET App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.

| SimpleOpenTelemetry | OpenTelemetry SDK family |
| ------------------- | ------------------------ |
| 0.1.x               | 1.16.x                   |

If you find SimpleOpenTelemetry helpful, please kindly consider [buying me a ☕](https://ko-fi.com/degero) 🙏

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quickstart](#quickstart)
- [Releases](#releases)
- [Documentation](#documentation)
- [Supported OpenTelemetry components](#supported-opentelemetry-components)
- [Feedback](#feedback)
- [Contributing](#contributing)
- [License](#license)

## Quickstart

**Prerequisites:** Docker desktop

In an empty directory:

```
dotnet new mvc
dotnet add package SimpleOpenTelemetry
```

Copy the example [aspnetcore-appsettings.json](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/configuration/examples/localdev/aspnetcore-appsettings.json) to replace `appsettings.Development.json`

Copy the localdev dockercompose file [SimpleOpenTelemetry example jaeger-lgtm-otel-collector](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/example-apps/localdev/otel-servers/jaeger-lgtm-otel-collector/docker-compose.yaml) to a directory `docker\docker-compose.yaml`, in that directory run: `docker compose up`

In the .csproj file:

Add after the 'SimpleOpenTelemetry' package line, add the snippet lines [aspnetcore-csproj-snippet.xml](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/configuration/examples/localdev/aspnetcore-csproj-snippet.xml)

In Program.cs add:

```csharp
// At the top of the file
using SimpleOpenTelemetry.Extensions;

// After WebApplication.CreateBuilder()
builder.AddSimpleOpenTelemetry();
```

In your app directory start the app: `dotnet run` (in another shell)

Navigate to local [Grafana](http://localhost:3000/) and [Jaeger](http://localhost:16686/) to view telemetry from your app

Exit your `dotnet run` and `docker compose up`

In `docker` directory run `docker compose down` (note for full cleanup you will need open docker desktop and delete the volumes it creates)

## Releases

[Releases](http://github.com/degero/simpleopentelemetry/releases)

[CHANGELOG.md](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/CHANGELOG.md)

## Documentation

For documentation, configuration reference, example configs/snippets, localdev and cloud example apps, troubleshooting, and more see the SimpleOpenTelemetry [docs](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/README.md) and [README.md](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/README.md).

## Supported OpenTelemetry components

OpenTelemetry, OpenTelemetry-contrib and other 3rd parties have many otel components published as NuGet packages. For a list of OpenTelemetry packages you can use with SimpleOpenTelemetry see [SimpleOpenTelemetry supported otel components](./docs/otel-component-versions.md). ⚠️ **It is recommended you install these versions of component packages.** ⚠️

It is still possible to add other components not listed here, but only via the code-based OpenTelemetry fluent api.

## Feedback

For issues, feature requests etc please submit here: [SimpleOpenTelemetry issues](https://github.com/degero/simpleopentelemetry/issues/new)

## Contributing

Contributions are most welcome.

[CONTRIBUTING](https://github.com/degero/simpleopentelemetry/blob/main/CONTRIBUTING.md)

## License

MIT License - see [LICENSE](https://github.com/degero/simpleopentelemetry/blob/main/LICENSE) file for details.
