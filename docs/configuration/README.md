
# SimpleOpenTelemetry Configuration Overview

**IMPORTANT**: ⚠️ *Config keys and values are NOT CASE SENSITIVE* ⚠️

SimpleOpenTelemetry is made up of these key configurable components:

- Distributions
- Trace/Metric/Log Exporters
- Trace/Metric Instrumentation
- Trace/Metric/Log Extensions
- Custom meters
- Trace sources
- Resource detectors
- Samplers
- Extensions
- Exporters
 <br>
 <br>

Details on each of these components can be found in [OpenTelemetry.io docs/concepts](https://opentelemetry.io/docs/concepts/).

---


## Configuration sources

As SimpleOpenTelemetry uses dotnet's IConfiguration concepts and abstractions, it relies on the default configuration sources setup in generic host platforms to load in appsettings.json. Settings are loaded in a particular hierarchy noted [here](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#default-app-configuration-sources), meaning settings in the config file can be overridden via Env vars.

The configuration system also means you can also [add in other configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) before calling AddSimpleOpenTelemetry(). These are particularly useful for loading in sensitive values (keys, secrets etc).

As the IConfigurationProvider for environment variables is enabled by default, you can define all SimpleOpenTelemetry settings and OTEL_ env vars in environment variables or in the appsettings.json file .

For local development with sensitive values, it is recommended to take advantage of [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets). This can be used for any configuration below or


---


## Environment variables

When setting SimpleOpenTelemetry configuration as Environment variables use the __ seperator for the hierarchical structure eg SimpleOpenTelemetry:Trace:Options as SimpleOpenTelemetry__Trace__Options. See [MSLearn - configuration-keys-and-values](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#configuration-keys-and-values).


The OpenTelemetry OTEL_* environment variables / json config are partially supported (see details further below) and load in by default (as this is done by the underlying OpenTelemetry SDK registration) but for many components those settings can be defined explicitly for their signal type/functionality in the configuration file or in code using the OpenTelemetryBuilder returned from SimpleOpenTelemetry.

Some core and critical OTEL_ environment variables you can set in Env var or root appsettings.json value (* indicates a recommended setting to set):


- [*OTEL_SERVICE_NAME](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)
- [*OTEL_RESOURCE_ATTRIBUTES](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)
- [OTEL_TRACES_SAMPLER, OTEL_TRACES_SAMPLER_ARG](https://opentelemetry.io/docs/languages/dotnet/sampling/#environment-variable-configuration)
- [OTEL_METRICS_EXEMPLAR_FILTER](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#exemplar) (contrary to spec, examplars are off by default due to performance cost.)
- [OTEL_SDK_DISABLED](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)


<br>

⚠️ **IMPORTANT** ⚠️
<br/>

 The OpenTelemetry Documentation [SDK Environment Variables](https://OpenTelemetry.io/docs/specs/otel/configuration/sdk-environment-variables) page is a specification not a reference for the dotnet implementation. Many of these are (as of july 2026) unsupported such as **OTEL_PROPAGATORS, OTEL_TRACES_EXPORTER, OTEL_LOGS_EXPORTER, OTEL_METRICS_EXPORTER**
> If you wish to make use of any of the environment variables in the spec but not above, check the [dotnet documentation to confirm it is implemented](https://OpenTelemetry.io/docs/languages/dotnet/getting-started/), or even quicker too dive into the [OpenTelemetry-dotnet repo](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main) to search.
>


---


## Configuration testing, debugging and deployment

Rather than building a configuration from scratch, you may want to start with one of the [example configs](./examples/) to find the configuration that is closest to your needs. For initial file setup and testing you can use a [example-apps/localdev/](../../example-apps/localdev/) with a local grafana LGTM to view telemetry or use one of the [example-apps/cloud/](../../example-apps/cloud/) and deploy to you cloud provider.


## Configuration file setup

**IMPORTANT**: ⚠️ *SimpleOpenTelemetry will emit error events and skip its setup if key settings are missing or misconfigured.* ⚠️

To get started, add a "SimpleOpenTelemetry" section to the root of your appsettings.json / appsettings.{Environment}.json file in your project folder. SimpleOpenTelemetry will set up all the components with OpenTelemetry for your application. If this is not set it will not run AddOpenTelemetry() with your application.

Similarly for the subsections "Metric/Trace/Log", OpenTelemetry's WithLogging/Tracing/Metrics() extension methods will only run (and  subsequent exports etc) when the corresponding section exists. If at least on is not set it will not run AddOpenTelemetry() with your application.

For a json configuration file, you can start with a full pre-built configuration in [examples](./examples/) or add in using [snippets](./snippets) or setup the top level config items and follow the next sections covering the items you can add:


```json
"SimpleOpenTelemetry": {
    "Distro": "",
    "DistroOptions": {},
    "Trace": {
      "Instrumentations": [],
      "InstrumentationConfig": {},
      "Sources": [],
      "Exporters": [],
      "Extensions": [],
      "Propagators": [],
      "Settings": {}
    },
    "Metric": {
      "Instrumentations": [],
      "InstrumentationConfig": {},
      "Exporters": [],
      "Settings": {},
      "CustomMeters": []
    },
    "Log": {
      "Exporters": [],
      "Settings": {}
    },
    "ExporterOptions": {},
    "Resource": {
      "Detectors": [ ],
      "DetectorConfig": {}
    },
    "Sampler": "",
    "BuilderExtensions": []
}

```


---


## Telemetry signal settings


OpenTelemetry signal handling is enabled via setting `SimpleOpenTelemetry:[Metrics/Tracing/Logging]` as `{}`, this will register OpenTelemetry's WithMetrics()/WithTracing()/WithLogging(). Omitting any of these will not register the collection / exporting or that signals telemetry. As en example, if your cloud provider is already collecting all the metrics you need you may opt to not collect them here.

Normally you will want to add other components for each signal, most importantly an exporter like OTLP. These are covered in the [configuration-component-details](#configuration-component-details)

Below covers information about each signal collection settings and documentation.


## Logging

Logging providers are not cleared by SimpleOpenTelemetry, but one will be added if the SimpleOpenTelemetry:Log section is defined. If you wish to have only use this logging provider and not the defaults in a Generic host application run `builder.Logging.ClearProviders()` before AddSimpleOpenTelemetry() as you can see in the [examples](./example-apps/localdev/).


## Settings

- IncludeFormattedMessage - bool (default: false)
- IncludeScopes - bool (default: false)
- ParseStateValues - bool (default: false)

[View OpenTelemetryLoggerOptions.cs for settings details](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Logs/ILogger/OpenTelemetryLoggerOptions.cs)


---


## Metrics

## Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via "SimpleOpenTelemetry:Metric:Settings":

- MetricLimit - int (default: 1000)

OpenTelemetry Documentation: [opentelemetry.io metrics best practices](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices)


---


## Tracing

Tracing in OpenTelemetry dotnet sdk defaults to `parentbased_always_on` meaning 100% of traces are emitted. For production environments, a sampling strategy should be in place either at the app side, collector side or both.


## Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via "SimpleOpenTelemetry:Trace:Settings":

- SetErrorStatusOnException - bool (default: false)

OpenTelemetry Documentation: [opentelemetry.io traces reporting exceptions](https://opentelemetry.io/docs/languages/dotnet/traces/reporting-exceptions/)


---


## Configuration component details


While all OpenTelemetry components in [OpenTelemetry-dotnet-contrib](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib) distros, and vendor implementations of components *could* be loaded using SimpleOpenTelemetry's configuration syntax, these are gated through registered assembly sets in the below folders to ensure those configurations have been tested in this repo:

- [Distros](./src/SimpleOpenTelemetry/OtelComponents/Distro/DistroAssemblies.cs)
- [Exporters](./src/SimpleOpenTelemetry/OtelComponents/Exporter/ExporterAssemblies.cs)
- [Trace / Metric Instrumentations](./src/SimpleOpenTelemetry/OtelComponents/Instrumentation/InstrumentationAssemblies.cs)
- [Extensions](./src/SimpleOpenTelemetry/OtelComponents/Extensions/ExtensionAssemblies.cs)
- [Samplers](./src/SimpleOpenTelemetry/OtelComponents/Sampler/SamplerAssemblies.cs)
- [Propagators](./src/SimpleOpenTelemetry/OtelComponents/Propagator/PropagatorAssemblies.cs)
- [Resource Detectors](./src/SimpleOpenTelemetry/OtelComponents/Resource/ResourceDetectorAssemblies.cs)

<br>

The next sections cover setting up the subsections of your "SimpleOpenTelemetry" config and details config information for components supported and snippets where options are available.

- [Distros](./distros.md)
- [Instrumentation](./instrumentations.md)
- [Exporters](./exporters.md)
- [Resource Detectors](./resource-detectors.md)
- [Propagators](./propagators.md)
- [Samplers](./samplers.md)
- [Extensions](./extensions.md)


---


## If a component isn't available/configurable


If a component type (eg. Processors), extension or setting isn't available to configure you can load/configure it in code using the OpenTelemetryBuilder returned from AddSimpleOpenTelemetry(). To search for a component check the [OpenTelemetry Registry](https://OpenTelemetry.io/ecosystem/registry/).

If what you need isn't available there, you can build your own following the OpenTelemetry guidelines for extending [traces](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/trace/extending-the-sdk/README.md) [logs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/logs/extending-the-sdk/README.md) and [metrics](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/metrics/extending-the-sdk/README.md).

If there is a registy component you would like added, feel free to raise a PR, or [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new).


---


## Production use tips


- If you are not sending telemetry to an OpenTelemetry Collector with some sampling in place, ensure you have a optimised sampler set in *OTEL_TRACES_SAMPLER* with a *OTEL_TRACES_SAMPLER_ARG* or code, as traces can be costly. See [OpenTelemetry - Sampling production guidance  ](https://opentelemetry.io/docs/languages/dotnet/sampling/#production-guidance)

- *OTEL_SERVICE_NAME* and *OTEL_RESOURCE_ATTRIBUTES* should always be set. This is best as Env vars for your deployed environments.

- Review the OpenTelemetry Best Practices doco for [Traces](https://opentelemetry.io/docs/languages/dotnet/traces/best-practices/), [Logs](https://opentelemetry.io/docs/languages/dotnet/logs/best-practices/) and [Metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/)

- Review the OpenTelemetry dotnet doco for best practices [Tracesw](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace), [Logs](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs) and [Metrics](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics)


---


## Best practices and Troubleshooting

Best practices and troubleshooting your configuration can be found in the main [README.md](../../README.md)
