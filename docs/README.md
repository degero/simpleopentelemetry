# SimpleOpenTelemetry Documentation

SimpleOpenTelemetry aids OpenTelemetry's code-based app instrumentation via dotnet's IConfiguration. If you are unfamiliar with OpenTelemetry or it's different ways of instrumenting apps, see the [What is OpenTelemetry](https://opentelemetry.io/docs/what-is-opentelemetry/) guide.

## Getting Started

### Quickstart

See the [Nuget package Quickstart guide](https://www.nuget.org/packages/SimpleOpenTelemetry#quickstart) to setup in a local aspnetcore mvc app using SimpleOpenTelemetry with Grafana LGTM running in docker to view telemetry.

### With an example app

You can use one of the [localdev example applications](../example-apps/localdev/README.md) or [cloud specific example apps](../example-apps/cloud/) (that can be deployed to the cloud, some can be run locally) with all the below code / configuration setup done. There is also [documentation](../example-apps/localdev/README.md#viewing-telemetry) in the localdev example applications to run locally and send telemetry to cloud / 3rd party telemetry services.

### With a new / existing dotnet app

⚠️ **There are OpenTelemetry package dependencies of SimpleOpenTelemetry pinned at a specific versions. See the main [README.md dependencies](../README.md#dependencies) for further information** ⚠️

- Add the SimpleOpenTelemetry nupkg: `dotnet add package SimpleOpenTelemetry`
- Add to the root of your `appsettings.{environment}.json`:
  ```json
  "OTEL_SERVICE_NAME": "yourappname",
  "OTEL_RESOURCE_ATTRIBUTES": "service.version=1.0.0,service.namespace=yourservicenamespace,deployment.environment.name=dev",
  "SimpleOpenTelemetry": {}
  ```
- For extra trace info to appear in your telemetry set Default loglevel to trace:

  ```json
  "Logging": {
      "LogLevel": {
        "Default": "Trace"
      }
  }
  ```

- Add bootstrapping code
  - For Generic Host apps like aspnetcore (or any apps like console using WebApplicationBuilder/HostApplicationBuilder):
    - In your startup code (eg Program.cs) add `using SimpleOpenTelemetry.Extensions;` and before builder.build() add `builder.AddSimpleOpenTelemetry();`

    - Optionally, add `builder.Logging.ClearProviders();` before this to clear all default WebApplicationBuilder/HostApplicationBuilder loggers and use just the logger to OpenTelemetry. This may be best to do if console / std logging is enabled on a cloud hosting platform.

    - Optionally, to validate OpenTelemetry has the key otel resource attributes and service.name set, run `app.Services.SimpleOpenTelemetryValidate();` after `var app = builder.Build();`. This writes any errors to the EventLog and returns false if invalid.

  - For Standalone apps (no Generic host):
    - In your startup code file add `using Microsoft.Extensions.Logging; using SimpleOpenTelemetry;` and

    ```csharp
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddOpenTelemetry();
        // You may want to set log levels using builder.AddConfiguration()
    });

    var sdk = SimpleOpenTelemetryBootstrap.Add(config);

    // on shutdown to push telemetry before closing - you can also access this via Bootstrap.Sdk
    sdk.Dispose();

    ```

  - Optionally, to validate OpenTelemetry has the key otel resource attributes and service.name set, run `app.Services.SimpleOpenTelemetryValidate();` after `var sdk = SimpleOpenTelemetryBootstrap.Add(config);`. This writes any errors to the EventLog and returns false if invalid if you wish to throw a unhandled exception.

- Read the next sections for configuration guidance and snippets or [example configurations](./configuration/examples/) to setup the SimpleOpenTelemetry section.

## How SimpleOpenTelemetry initialises OpenTelemetry

`AddSimpleOpenTelemetry()` initialises OpenTelemetry with either the service collection extension `AddOpenTelemetry()` for generic host or `OpenTelemetrySdk.Create()` for standalone apps. It will then process the configuration and call the OpenTelemetryBuilder fluentapi methods to configure settings and components.

For more detail on OpenTelemetry's two methods of initialisation see:

- [Initialize the SDK using a host](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/README.md#initialize-the-sdk-using-a-host)

- [Initialize the SDK manually](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/README.md#initialize-the-sdk-manually)

## Configuration

If you wish to skip reading the configuration information for now see [example configs](./configuration/examples/) to find the configuration that closest suits your cloud / 3rd party telemetry services needs. You may wish to check this later to find any other instrumentation features you can make use of.

See [configuration/README.md](./configuration/README.md) for full details of each configuration area.

## Consuming App Telemetry

There are many destinations you can export your telemetry to. The [example-apps/cloud](../example-apps/cloud/) and [configuration//examples/](./configuration/examples/) cover cloud environments and 3rd party services. The [example-apps/localdev/](../example-apps/localdev/) show using local Grafana LGTM and Jaeger.

## Instrumenting your apps

Telemetry can be quite costly (especially traces) in hosting costs, resource needs, performance and storage depending on the scale of your app. Ensure you only gather what you identify as important for your monitoring / alerting needs and ensure sampling settings are in place for production environments.

You can alter your app in the below areas to utilise all the telemetry features

### Logging

Logging to OpenTelemetry can be done with a standard dotnet ILogger<> with all the log levels supported.

Using the [Logging setting](#logging) `IncludeFormattedMessage` is recommended if using parameterised logging eg `_logger.LogInformation("Test message. {Action}",action);` and you want parameter easy to query in your monitoring platform.

### Distributed Tracing

Additionally to the trace instrumentation libraries covered in the SimpleOpenTelemetry configuration documentation, you can generate custom traces. See the [example aspnetcore app HomeController](../example-apps/localdev/aspnetcore/Controllers/HomeController.cs) for a custom trace example. This requires an `SimpleOpenTelemetry:Trace:Sources[]` entry with the source name or wildcard, see [example aspnetcore app appsettings.Example.json](../example-apps/localdev/aspnetcore/appsettings.Example.json).

Using the [Trace setting](#tracing) `SetErrorStatusOnException` as `true` is recommended to record an trace status as `Error` automatically when an exception is thrown in a trace. If you need more detail than a bool it can be recorded in a catch statement

For an example of all the dotnet tracing features available see [MSLearn - Adding distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)

### Metrics

Several Dotnet SDK libs generate metrics which is usually configured to be collected by adding metric instrumentation libraries covered in the SimpleOpenTelemetry configuration documentation.

`SimpleOpenTelemetry:Meter:CustomMeters[]` json array allows adding other meter collections from libraries that are generating them or to collect a custom meter you have created in your app. To create these, see [MSLearn - Creating Metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation).

## Production Tips and Best practices

- For hosted environments, it is highly recommended to use an OpenTelemetry collector for between your app and your telemetry destination.

- _OTEL_SERVICE_NAME_ and _OTEL_RESOURCE_ATTRIBUTES_ settings should always be defined and better controlled via Env vars in your deployed environment

- If you are not sending telemetry to an OpenTelemetry Collector with some sampling in place there, ensure you have a optimised sampler set in _OTEL_TRACES_SAMPLER_ with a _OTEL_TRACES_SAMPLER_ARG_ or code, as collecting 100% of traces can be costly. Even if you do have a collector, consider sampling at the app side to reduce traffic as this may become quite heavy at scale. See [OpenTelemetry - Sampling production guidance](https://opentelemetry.io/docs/languages/dotnet/sampling/#production-guidance)

- See the cloud examples for tips on that specific environment [example-apps/cloud](../example-apps/cloud/)

- Review the OpenTelemetry Best Practices doco for [Traces](https://opentelemetry.io/docs/languages/dotnet/traces/best-practices/), [Logs](https://opentelemetry.io/docs/languages/dotnet/logs/best-practices/) and [Metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/)

- Review the OpenTelemetry dotnet doco for best practices [Traces](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace), [Logs](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs) and [Metrics](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics)

## SimpleOpenTelemetry Error handling and Diagnostics

## Error handling

SimpleOpenTelemetry follows the same [spec guideline](https://opentelemetry.io/docs/specs/otel/error-handling/) as OpenTelemetry for error handling in that it _'MUST NOT throw unhandled exceptions at runtime.'_. Building on that, it will not throw any unhandled exceptions if a configuration does not work (eg config env var / files change), it will not prevent the app from running. SimpleOpenTelemetry WILL throw exceptions for null parameters passed to it's registration methods.

## Diagnostics

SimpleOpenTelemetry records logs or errors as diagnostics events (as OpenTelemetry does). Note that emitted "SimpleOpenTelemetry-" prefixed events only occur at the app startup and will only emit if a listener is registered before calling `AddSimpleOpenTelemetry()`.

Projects in the [localdev example apps](../example-apps/localdev/) folder demonstrate custom code (SimpleOtelEventListener, OtelEventListener) listening to the "SimpleOpenTelemetry-" and "OpenTelemetry-" events and outputting to console. This maybe useful to adapt from and use if you app environment only has stdout as a means to view events.

Some options to listen to events if not using a code based event listener/console output in the examples:

### Using dotnet-trace

With a published app:

```powershell
dotnet tool install --global dotnet-trace
dotnet-trace collect --providers "SimpleOpenTelemetry-Core:0xFFFFFFFF:5" -- dotnet .\AspNetCore.dll
```

### Using Perfview

1. install PerfView: `winget install PerfView`
2. Run as administrator
3. Menu 'collect' -> 'collect'
4. Uncheck all in the section starting 'Kernel base'
5. Type '\*SimpleOpenTelemetry-Core'
6. Click 'Start Collection'
7. Start the app and interact with it
8. Click 'Stop Collection' and close this windows
9. View events in the datafile created

Information on collecting OpenTelemetry events: [OpenTelemetry Troubleshooting](https://opentelemetry.io/docs/languages/dotnet/troubleshooting/)

You can also make use of OpenTelemetry's diagnostics writer. This writes any diagnostics to log files. You can place a OTEL_DIAGNOSTICS.json file in the apps working directory.
[OpenTelemetry-dotnet self-diagnostics](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#self-diagnostics)
