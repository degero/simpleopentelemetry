# SimpleOpenTelemetry Appsettings Configs for Azure AppService AspNetCore 

These configs cover using Azure's [ASPNetCore](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/README.md) and [Exporter](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/README.md) libraries. The former adds extra features such as extra instrumentations (httpclient, sqlcient), resource detectors and logging automatically and the later config replicates these. There are some quirks to the AspNetCore distro so you may want to use the exporter for more control or exporter by signal (with the loss of live metrics offline storage etc). `aspnetcore-azureotel-exporter-rbac.json` includes live metrics

These configurations use RBAC to send telemetry to azure monitor. See here for [setup guide](https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-ad-authentication?tabs=aspnetcore#configure-and-enable-microsoft-entra-id-based-authentication) or skip to the notes below on how to use just a plain connection string.

If running with RBAC locally (the default of these configs) you will need to assign your azure user with role mentioned in the above doco.

## Packages

### Required for all

`dotnet add package SimpleOpenTelemetry`
`dotnet add package Azure.Identity` (if using RBAC to connect to azure monitor)

### Required for aspnetcore-azureotel-distro-rbac.json file:

Adjust your config related to the optional instrumentations

**optional**
`dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore`   
`dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore`

*IMPORTANT*

If you add package `OpenTelemetry.Instrumentation.SqlClient` you will need to configure it by code. As the distro will backoff from setting up its own internal sqlclient instrumentation if it detects it.


### Required for aspnetcore-azureotel-exporter*rbac.json files

These are the core packages as distro uses, adjust your config related to the optional instrumentations

`dotnet add package Azure.Monitor.OpenTelemetry.Exporter`  
`dotnet add package OpenTelemetry.Instrumentation.Http`

**optional**
`dotnet add package OpenTelemetry.Instrumentation.AspNetCore`
`dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore`
`dotnet add package OpenTelemetry.Instrumentation.SqlClient`


## Appsettings file

The ConnectionString is a placeholder for dev environments so the library will not throw an exception. This should be removed on deployments and set via secret Env vars in hosted environments.

Customise:

 - Set OTEL_RESOURCE_ATTRIBUTES values and OTEL_SERVICE_NAME with your preferred names. See library quirks below regarding these attributes to ensure all are applied.
 - Trace:Sources: Replace 'yourappnamespace' with your apps root namespace or remove this if you don't have any custom diagnostics events or if using aspnetcore distro you can use the builtin trace source (see doco)
 - Remove any instrumentations you may not need (and their respective nuget package).
 - Adjust logging settings and SetErrorStatusOnException (see notes below)


## Other options and Sampling

Options can be set with the settings file (eg 'EnableTraceBasedLogsSampler' correlates to the code based Action<> options) and sampling by either options or ENV vars. For more details on Sampling settings see [Enable sampling](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore#enable-sampling)

Sampling in these files are set to a MS recommended very safe 5% fixed percentage. But you should read the above and adjust based on your usage scenario.

## Environment variables

Ensure you set the following recommended item as an Environment variable (this cannot be done in appsettings file).

```
  "APPLICATIONINSIGHTS_STATSBEAT_DISABLED": "true"
```

### Required for aspnetcore-azureotel-distro-rbac.json file:

Set a secret/env var for 'SimpleOpenTelemetry__DistroOptions__ConnectionString'
eg `InstrumentationKey=<yourinstrumentationkey>`


### Required for aspnetcore-azureotel-exporter-rbac.json file:

Set a secret/env var for 'SimpleOpenTelemetry__BuilderExtensions__0__Options__ConnectionString'
eg `InstrumentationKey=<yourinstrumentationkey>`

### Required for aspnetcore-azureotel-exporter-by-signal-rbac.json file:

Set a secret/env var for 'SimpleOpenTelemetry__ExporterOptions__AzureMonitor__ConnectionString'
eg `InstrumentationKey=<yourinstrumentationkey>`


## Azure AppInsights authentication

If you wish to NOT use RBAC for the lib to authenticate with AppInsights set a full Application Insights connection string. Remove the 'Credential' setting and ensure the environment has the full connectionstring in the respective env var connection string noted before.


## Configuration notes

The 'SetErrorStatusOnException' setting will set Success = false on Application insights 'dependencies' table entries when an exception occurs during an System.Diagnostics.Event.

Adjust your sampling settings as required, for more information see [MSLearn - Enable sampling](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore#enable-sampling)

Log settings for the Azure monitor exporter match what the ASPNetCore distro sets.

Other trace instrumentations that may be useful in the this hosting scenario for deeper metrics:

- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Instrumentation.Process


## Library Quirks

### Custom Attributes

There is a ['feature request'](https://github.com/Azure/azure-sdk-for-net/issues/46020) regarding custom resource attributes (OTEL_SERVICE_ATTRIBUTES) not appearing in Azure Monitor.

### Resource name

OTEL_SERVICE_NAME will be overwritten by Azure's resource detector setting service.name as the azure resource name. If you wish it to be your own name see [SimpleOpenTelemetry README](../../../README.md#azure)
