# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry via IConfiguration, supporting both generic-host and standalone apps. Example config snippets and configurations for major cloud platforms can be dropped in easily and the underlying OpenTelemetryBuilder stays accessible for adding settings via code.

## Requirements

- .NET 8.0, 10.0


## Compatibility

| SimpleOpenTelemetry | OpenTelemetry SDK family | Microsoft.Extensions.Logging |
|---|---|---|
| 0.1.0 | 1.16.x | 10.0.x |

These dependencies are included in the package. There are also Microsoft.* are transitive deps from OpenTelemetry SDK Family.


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

For configuration reference, example configs/snippets, cloud examples, and troubleshooting, see the SimpleOpenTelemetry [README.md](https://github.com/degero/simpleopentelemetry/blob/main/README.md) and [docs](https://github.com/degero/simpleopentelemetry/blob/main/docs/README.md)


## License

MIT License - see [LICENSE](./LICENSE) file for details.


## Feedback

For issues, feature requests, or contributions, visit:
https://github.com/degero/SimpleOpenTelemetry
