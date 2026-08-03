# SimpleOpenTelemetry Documentation

If you are unfamiliar with OpenTelemetry or it's different ways of instrumenting apps, see the [What is OpenTelemetry](https://opentelemetry.io/docs/what-is-opentelemetry/) guide.


## Getting Started

### With an example app

You can use one of the [localdev example applications](./example-apps/localdev/README.md) or [cloud specific example apps](../example-apps/cloud/) (that can be deployed immediately to the cloud) with all the below code / configuration setup done.


### With a new / existing dotnet app

- Add the SimpleOpenTelemetry nupkg: `dotnet add package --prerelease SimpleOpenTelemetry`
- Add a "OTEL_SERVICE_NAME", "OTEL_RESOURCE_ATTRIBUTES" and "SimpleOpenTelemetry": {} to the root of your appsettings.{environment}.json and read the next sections or [example configrations](./configuration/examples/) to setup.
- Add boostrapping code:


  - For Generic Host apps like aspnetcore (or any apps like console using WebApplicationBuilder/HostApplicationBuilder):

    - In your startup code (eg Program.cs) add `using SimpleOpenTelemetry.Extensions;` and before builder.build() add `builder.AddSimpleOpenTelemetry();`

    - Optionally, add `builder.Logging.ClearProviders();` before this to clear all default WebApplicationBuilder/HostApplicationBuilder loggers and use just the logger to OpenTelemetry. This may be best to do if console / std logging is enabled on a cloud hosting platform.

    - Optionally, to validate OpenTelemetry have the key app identifiers set, run `app.Services.SimpleOpenTelemetryValidate();` after `var app = builder.Build();`. This writes any errors to the EventLog and returns false if invalid.


  - For Standalone apps (no Generic host):
    - In your startup code file add `using Microsoft.Extensions.Logging; using SimpleOpenTelemetry;` and
    ```csharp
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddOpenTelemetry();
        // You may want to set log levels using builder.AddConfiguration()
    });

    var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);

    // on shutdown to push telemetry before closing - you can also access this via StandaloneApp.Sdk
    sdk.Dispose();

    ```
  - Optionally, to validate OpenTelemetry have the key app identifiers set, run `app.Services.SimpleOpenTelemetryValidate();`   after `var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);`. This writes any errors to the EventLog and returns false if invalid if you wish to throw a unhandled exception.


## How SimpleOpenTelemtry initialises OpenTelemetry

`AddSimpleOpenTelemetry()` initialises OpenTelemetry with either the service collection extension `AddOpenTelemetry()` for generic host or `OpenTelemetrySdk.Create()` for standalone apps. It will then process the configuration and call the OpenTelemetryBuilder fluentapi methods to configure settings and components.

For more detail on OpenTelemetry's two methods of initialisation see:

  - [Initialize the SDK using a host](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/README.md#initialize-the-sdk-using-a-host)

  - [Initialize the SDK manually](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/README.md#initialize-the-sdk-manually)


---


## Configuration

If you wish to skip reading the configuration information for now see [example configs](./configuration/examples/) to find the configuration that closest suits your needs. You may wish to check this later to find any other instrumentation features you can make use of.

See [configuration/README.md](./configuration/README.md) for full details of each configuration area.


---


## Instrumenting your apps

Telemetry can be quite costly (especially traces) in hosting costs, resource needs, performance and storage depending on the scale of your app. Ensure you only gather what you identify as important for your monitoring / alerting needs and ensure sampling settings are in place for production environments.

You can alter your app in the below areas to utilise all the telemetry features


### Logging

Logging to OpenTelemetry can be done with a standard dotnet ILogger<> with all the log levels supported.

Using the [Logging setting](#logging) `IncludeFormattedMessage` is recommended if using parameterised logging eg `_logger.LogInformation("Test message. {Action}",action);` and you want parameter easy to query in your monitoring platform.


### Distributed Tracing

Additionally to the trace instrumentation libraries covered in the SimpleOpenTelemetry configuration documentation, you can generate custom traces. See the [example aspnetcore app HomeController](./example-apps/localdev/aspnetcore/Controllers/HomeController.cs) for a custom trace example. This requires an `SimpleOpenTelemetry:Trace:Sources[]` entry with the source name or wildcard, see [example aspnetcore app appsettings.Example.json](./example-apps/localdev/aspnetcore/appsettings.Example.json).


Using the [Trace setting](#tracing) `SetErrorStatusOnException` as `true` is recommended to record an trace status as `Error` automatically when an exception is thrown in a trace. If you need more detail than a bool it can be recorded in a catch statement

For an example of all the dotnet tracing features available see [MSLearn - Adding distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)


### Metrics

Several Dotnet SDK libs generate metrics which is usually configured to be collected by adding metric instrumentation libraries covered in the SimpleOpenTelemetry configuration documentation.

`SimpleOpenTelemetry:Meter:CustomMeters[]` json array allows adding other meter collections from libraries that are generating them or to collect a custom meter you have created in your app. To create these, see [MSLearn - Creating Metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation).


---


## SimpleOpenTelemetry Error handling, Diagnostics and Troubleshooting

SimpleOpenTelemetry follows the same [spec guideline](https://opentelemetry.io/docs/specs/otel/error-handling/) as OpenTelemetry for error handling in that it 'MUST NOT throw unhandled exceptions at runtime.'. Building on that it will not throw any errors if a configuration does not work (eg config env var / files change) it will not prevent the app from running. Note that emitted "SimpleOpenTelemetry-" prefixed events only occur at the app startup and will only emit if a listener is registered before starting.

SimpleOpenTelemetry will throw exceptions for null parameters passed to it's registration methods. SimpleOpenTelemetry records any errors as diagnostics events (as OpenTelemetry does).  Projects in the [examples](./example-apps/localdev/) folder demonstrate custom code listening to this and "OpenTelemetry-" events and outputting to console. This maybe useful to adapt from and use if you app environment only has stdout as a means to view events.

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
5. Type '*SimpleOpenTelemetry-Core'
6. Click 'Start Collection'
7. Start the app and interact with it
6. Click 'Stop Collection' and close this windows
8. View events in the datafile created


Information on collecting OpenTelemetry events: [OpenTelemetry Troubleshooting](https://opentelemetry.io/docs/languages/dotnet/troubleshooting/)

You can also make use of OpenTelemetry's diagnostics writer. This writes any diagnostics to log files. You can place a OTEL_DIAGNOSTICS.json file in the apps working directory.
[OpenTelemetry-dotnet self-diagnostics](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#self-diagnostics)


---

