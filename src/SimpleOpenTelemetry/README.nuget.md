# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry code-based instrumentation via IConfiguration, supporting both generic-host and standalone apps. Pre-tested example config snippets and configurations for major cloud platforms can be dropped in easily and the underlying OpenTelemetryBuilder stays accessible for adding settings via code.

**Supported Frameworks:** .NET 10.0, .NET 8.0

**Supported .Net App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.

**License:** MIT

[CHANGELOG.md](./CHANGELOG.md)

## Compatibility

| SimpleOpenTelemetry | OpenTelemetry SDK family |
| ------------------- | ------------------------ |
| 0.1.0               | 1.16.x                   |

These dependencies are included in the package. There are also Microsoft.\* are transitive deps from OpenTelemetry SDK Family.

## Requirements

- Docker desktop (to get started with the quickstart)

## Quickstart

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

## Documentation

For configuration reference, example configs/snippets, cloud examples, and troubleshooting, see the SimpleOpenTelemetry [README.md](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/README.md) and [docs](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/README.md)

## Supported OpenTelemetry components

OpenTelemetry, OpenTelemetry-contrib and other 3rd parties have many otel components published as NuGet packages. For a list of supported / tested OpenTelemetry packages you can plug in see [SimpleOpenTelemetry tested otel components](https://github.com/degero/simpleopentelemetry/blob/{{TAG}}/docs/otel-component-versions.md).

⚠️ **Ensure you install these versions of component packages or use the latest version at your own risk.** ⚠️

## License

MIT License - see [LICENSE](https://github.com/degero/simpleopentelemetry/blob/main/LICENSE) file for details.

## Feedback

For issues, feature requests etc please submit here: [SimpleOpenTelemetry issues](https://github.com/degero/simpleopentelemetry/issues/new)

## Contributing

Contributions are most welcome.

[CONTRIBUTING](https://github.com/degero/simpleopentelemetry/blob/main/CONTRIBUTING)
