# SimpleOpenTelemetry

A lightweight, low-code .NET library for configuring OpenTelemetry via IConfiguration, supporting both generic-host and standalone apps. Example config snippets and configurations for major cloud platforms can be dropped in easily and the underlying OpenTelemetryBuilder stays accessible for adding settings via code.

**Supported Frameworks:** .NET 10.0, .NET 8.0


## Quickstart


```
dotnet add package --prerelease SimpleOpenTelemetry
```

Add a `"SimpleOpenTelemetry": {}` section to the root of your `appsettings.{environment}.json`.

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

var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);

// on shutdown, to flush telemetry before closing
sdk.Dispose();
```


## Documentation

For configuration reference, example configs/snippets, cloud examples, and troubleshooting, see the SimpleOpenTelemetry [README.md](https://github.com/degero/simpleopentelemetry/blob/main/README.md) and [docs](https://github.com/degero/simpleopentelemetry/blob/main/docs/README.md)


## License

MIT License - see [LICENSE](https://github.com/degero/simpleopentelemetry/blob/main/LICENSE) file for details.


## Feedback

For issues, feature requests, or contributions, visit:
https://github.com/degero/SimpleOpenTelemetry
