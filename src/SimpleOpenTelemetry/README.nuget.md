# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry code-based instrumentation via IConfiguration, supporting both generic-host and standalone apps. Pre-tested example config snippets and configurations for major cloud platforms can be dropped in easily and the underlying OpenTelemetryBuilder stays accessible for adding settings via code.

**Supported Frameworks:** .NET 10.0, .NET 8.0

**Supported .Net App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.

**License:** MIT

[CHANGELOG.md](./CHANGELOG.md)


## Compatibility

| SimpleOpenTelemetry | OpenTelemetry SDK family |
|---|---|
| 0.1.0 | 1.16.x |

These dependencies are included in the package. There are also Microsoft.* are transitive deps from OpenTelemetry SDK Family.


## Requirements

None. You can choose components to 'plug-in' below.


## Quickstart


```
dotnet add package SimpleOpenTelemetry
```

Add `"OTEL_SERVICE_NAME": ""`, `"OTEL_RESOURCE_ATTRIBUTES": ""` and `"SimpleOpenTelemetry": {}` section to the root of your `appsettings.{environment}.json`.

**Generic Host apps** (aspnetcore, or any app using `WebApplicationBuilder`/`HostApplicationBuilder`):

```csharp
using SimpleOpenTelemetry.Extensions;

// before builder.Build()
builder.AddSimpleOpenTelemetry();
```


**Standalone apps** (no generic host):

```csharp
using Microsoft.Extensions.Logging;
using SimpleOpenTelemetry;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddOpenTelemetry();
});

var sdk = SimpleOpenTelemetryBootstrap.Add(config);

// on shutdown, to flush telemetry before closing
sdk.Dispose();
```


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
