# SimpleOpenTelemetry AppSettings Configs for Azure

These configs cover using Azure's [ASPNetCore](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/README.md) and [Exporter](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/README.md) libraries.

The former is a full feature set, the later config replicates these features where possible. There are [some quirks](#library-quirks) to the AspNetCore distro so you may want to use the exporter for more control or exporter by signal (with the loss of live metrics).

Before you decide if you want to use Azure Montor's Distro of OpenTelemetry or just a regular [OTLP exporter](../../exporters.md#otlp-exporter) see microsoft's [why-should-i-use-the-azure-monitor-opentelemetry-distro](https://learn.microsoft.com/en-us/azure/azure-monitor/app/application-insights-faq#why-should-i-use-the-azure-monitor-opentelemetry-distro)

Note the term distro in SimpleOpenTelemetry means a full OpenTelemetry solution that sets up all key components and all signal exporting. Both the AspNetCore and Exporter libraries are part of microsoft's OpenTelemetry distro.

## Included files

- `aspnetcore-azureotel-distro-rbac.json`: exports telemetry for all signals, live metrics, offline storage resource detectors, logging setup all built into the lib
- `aspnetcore-azureotel-exporter-rbac.json` : lib exports telemetry for all signals, includes live metrics, offline storage. config manualy sets resource detectors
- `aspnetcore-azureotel-exporter-by-signal-rbac.json` : nothing automatically included in lib save for offline storage, has all manual setup for resource detectors, logging setup and export of all signals, which you can opt out of any of these.

These configs include registering all optional packages mentioned below and custom meters, adjust to your needs. They also defalt to using RBAC to send Telemetry to AppInsights. Both the Distro and the All signal exporter configuration will setup formatted log messages. This is also included in the `aspnetcore-azureotel-exporter-by-signal-rbac.json` config file as it does not enable by default.

## Packages

**IMPORTANT**: ⚠️ **It is recommended you install [these versions tested against SimpleOpenTelemetry](../../../otel-component-versions.md) of packages referenced below.** ⚠️

### Required for all configs

`dotnet add package SimpleOpenTelemetry`
`dotnet add package Azure.Identity` (if using RBAC to connect to azure monitor)

### Required for aspnetcore-azureotel-distro-rbac.json file:

Adjust your config related to the optional instrumentations

`dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore  --version x.x.x`

**optional**
`dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x`
`dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version x.x.x`
`dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x`
`dotnet add package OpenTelemetry.Instrumentation.SqlClient --version x.x.x`

This config uses `SimpleOpenTelemetry:DistroOptions:ConnectionString` for the appinsights connectionstring

_IMPORTANT_

If you add any of the optional packages you will need to configure it in the SimpleOpenTelemetry config (already included), if not remove the instrumentation entry for these names in the config.

While the distro does instrument some aspnetcore metrics, it is only a subset of the optional `OpenTelemetry.Instrumentation.AspNetCore` when running dotnet 8+, see the github repo for meters. If you want aspnetcore memory and kestrel meters included in dotnet 8+, add this by including the package and adding the [config](../../instrumentations.md#aspnetcore) or add these meters in your SimpleOpenTelemetry config.

### Required for aspnetcore-azureotel-exporter\*rbac.json files

These are the core packages as distro uses, adjust your config related to the optional instrumentations

`dotnet add package Azure.Monitor.OpenTelemetry.Exporter --version x.x.x`

**optional**
`dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x`
`dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x`
`dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version x.x.x`
`dotnet add package OpenTelemetry.Instrumentation.SqlClient --version x.x.x`

aspnetcore-azureotel-exporter-by-signal-rbac.json uses the connectionstring `SimpleOpenTelemetry:ExporterOptions:AzureMonitor:ConnectionString`

aspnetcore-azureotel-exporter-rbac.json uses the connectionstring `SimpleOpenTelemetry:BuilderExtensions:0:Options:ConnectionString`

## How to use

See the [example app](../../../../example-apps/cloud/azure/appservice/README.md) using these configurations

OR Follow the below:

Copy one of the examples to your `appsettings.Development.json` or `appsettings.TARGETENVIRONMENT.json` you are using

1. Set `OTEL_RESOURCE_ATTRIBUTES` values and `OTEL_SERVICE_NAME` with your preferred names. See library quirks below regarding these attributes to ensure all are applied.
1. In `Trace:Sources` replace 'yourappnamespace' with your apps root namespace or remove this if you don't have any custom diagnostics events or if using aspnetcore distro you can use the builtin trace source (see doco)
1. Remove any instrumentations you may not need (and their respective nuget package mentioned as the optional packages below).
1. Remove any custommeters you may not need
1. Adjust logging settings and SetErrorStatusOnException (see notes below)
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. If you wish to NOT use RBAC for the lib to authenticate with AppInsights remove the `Credential` config file setting.
1. Create an AppInsights instance and get your connection string [see here for scripts](../../../../example-apps/cloud/azure/appservice/README.md#local-run-with-selected-config), set using the configuration key needed by your chosen config mentioned before. See below for more detail on setting this based on your choice of RBAC or not.
1. Add `using SimpleOpenTelemetry.Extensions; builder.AddSimpleOpenTelemetry();` on your WebApplicationBuilder (eg Program.cs) before the builder.Build();
1. Run the app or deploy to app service
1. Confirm your telemetry in Azure Application Insights

## Appinsights connectionstring

The ConnectionString is a placeholder for dev environments so the library will not throw an exception when running with RBAC. This should ideally be removed on deployments and set via vault secrets / Env vars in hosted environments. You can use `dotnet user-secrets init` and `dotnet user-secrets set` for local dev to set your connection string.

For RBAC the connection string is just like the config file placeholder, but with a non-zero value instrumentation key from your appinsights. If not using RBAC, set a full Application Insights connection string.

If you opt for (default) RBAC, see here for [setup guide](https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-ad-authentication?tabs=aspnetcore#configure-and-enable-microsoft-entra-id-based-authentication) to setup access.

## Documentation

For detailed documentation on all the configurable components of the aspnetcore and exporter libs see [Configure Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore)

## Troubleshooting

- Ensure you have either a `appsettings.Development.json` for local dev or `appsettings.Production.json` for deployments
- Verify the Application Insights connection string or RBAC role `Monitoring Metrics Publisher` set on appinsights for the app service identity if telemetry does not appear.
- Note that live metrics works for all sample configuration except `aspnetcore-azureotel-exporter-by-signal-rbac.json`
- If using `DefaultAzureCredential` and running locally, ensure you are logged in with `az login` and have set the connectionstring to your appinsights instance instrumentation key.
- Validate `service.name` and custom resource values if telemetry metadata is not appearing as expected.
- The log analytics assignment may have issues on deployment where you will see an error when browsing telemetry 'Error retrieving data'. If you re-assign the workspace in the appinsights properties this should resolve.

## App Settings Configuration Reference

| App Setting Name                                                    | Value                        | Purpose                                                                      |
| ------------------------------------------------------------------- | ---------------------------- | ---------------------------------------------------------------------------- |
| `APPLICATIONINSIGHTS_STATSBEAT_DISABLED`                            | `true`                       | Disable Application Insights statsbeat (internal metrics) to reduce overhead |
| `OTEL_RESOURCE_ATTRIBUTES`                                          |                              | Add custom resource attributes for telemetry identification                  |
| `OTEL_SERVICE_NAME`                                                 | `soteltestazure`             | Service name for OpenTelemetry                                               |
| `SCM_DO_BUILD_DURING_DEPLOYMENT`                                    | `true`                       | Enable build during App Service deployment (if using Git/ZIP deployment)     |
| `SimpleOpenTelemetry:ExporterOptions:AzureMonitor:ConnectionString` | `InstrumentationKey=<key>`   | Application Insights instrumentation key (set by Terraform)                  |
| `SimpleOpenTelemetry:DistroOptions:ConnectionString`                | `InstrumentationKey=<key>`   | Application Insights instrumentation key (set by Terraform)                  |
| `SimpleOpenTelemetry:BuilderExtensions:0:Options:ConnectionString`  | `InstrumentationKey=<key>`   | Application Insights instrumentation key (set by Terraform)                  |
| `OTEL_TRACES_SAMPLER`                                               | `microsoft.fixed_percentage` | OpenTelemetry sampler                                                        |
| `OTEL_TRACES_SAMPLER_ARG`                                           | `0.05`                       | OpenTelemetry sampler arg (in this case 5% traces are consumed)              |

**Note:** In App Service environment variables, colons (`:`) in configuration keys are converted to double underscores (`__`). For example, `SimpleOpenTelemetry:ExporterOptions:AzureMonitor:ConnectionString` becomes `SimpleOpenTelemetry__ExporterOptions__AzureMonitor__ConnectionString` in the environment.

## Other options and Sampling

Options can be set with the settings file (eg 'EnableTraceBasedLogsSampler' correlates to the code based Action<> options) and sampling by either options or ENV vars. For more details on Sampling settings see [Enable sampling](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore#enable-sampling)

Sampling in these files are set to a MS recommended very safe 5% fixed percentage. But you should read the above and adjust based on your usage scenario.

## Environment variables

Ensure you set the following recommended item as an Environment variable (this cannot be done in appsettings file).

```
  "APPLICATIONINSIGHTS_STATSBEAT_DISABLED": "true"
```

## Configuration notes

The 'SetErrorStatusOnException' setting will set Success = false on Application insights 'dependencies' table entries when an exception occurs during an System.Diagnostics.Event.

Adjust your sampling settings as required, for more information see [MSLearn - Enable sampling](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore#enable-sampling)

Log settings for the Azure monitor exporter match what the ASPNetCore distro sets.

Other trace/metric instrumentations that may be useful in the this hosting scenario for deeper metrics:

- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Instrumentation.Process

See the [SimpleOpenTelemetry README.md](../../README.md#process-runtime) to set these up

A custom meter for System.Net.NameResolution is included as an example and can be removed if this metric is unneeded

## Library Quirks

### Custom Attributes

There is a ['feature request'](https://github.com/Azure/azure-sdk-for-net/issues/46020) regarding custom resource attributes (OTEL_SERVICE_ATTRIBUTES) not appearing in Azure Monitor.

### Resource name

OTEL_SERVICE_NAME will be overwritten by Azure's resource detector setting service.name as the azure resource name. If you wish it to be your own name see [SimpleOpenTelemetry README](../../../README.md#azure)
