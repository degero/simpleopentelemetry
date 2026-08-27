# SimpleOpenTelemetry Configuration

## Table of contents

- [Configuration structure](#configuration-structure)
- [Configuration file setup](#configuration-file-setup)
- [Enabling telemetry signal collection](#enabling-telemetry-signal-collection)
- [Logging](#logging)
- [Metrics](#metrics)
- [Tracing](#tracing)
- [OpenTelemetry component configuration](#opentelemetry-component-configuration)
- [Configuration testing, debugging and deployment](#configuration-testing-debugging-and-deployment)
- [Configuration sources](#configuration-sources)
- [Environment variables](#environment-variables)
- [OpenTelemetry environment variables](#opentelemetry-environment-variables)
- [Supported OpenTelemetry components](#supported-opentelemetry-components)

## Overview

SimpleOpenTelemetry configuration at a high level is is made up of settings and these key components:

- Distributions
- Trace/Metric/Log Exporters
- Trace/Metric Instrumentation
- Trace/Metric/Log Extensions
- Resource detectors
- Samplers
- Propagators
- Extensions

Details on each of these components can be found in [OpenTelemetry.io docs/concepts](https://opentelemetry.io/docs/concepts/).

While JSON file setup is the common means of configuring SimpleOpenTelemetry, and all examples / snippets are in this format, you can choose from other [IConfiguration providers](#configuration-sources).

SimpleOpenTelemetry will set up all the components with OpenTelemetry for your application by processing your configuration. If the `"SimpleOpenTelemetry"` config section is not set it will not run OpenTelemetry's `AddOpenTelemetry()` and emit a critical error event (not an exception).

Similarly for subsections including `Metric/Trace/Log`, OpenTelemetry's `WithLogging/Tracing/Metrics()` extension methods will only run (and subsequent exports etc) when the corresponding section exists. If at least one is not set it will not run AddOpenTelemetry() with your application and emit a critical error event.

For information on viewing these events see [Error handling and diagnostics](../README.md#simpleopentelemetry-error-handling-and-diagnostics)

## Configuration structure

**IMPORTANT**: ⚠️ _Config keys and values are NOT CASE SENSITIVE_ ⚠️

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
    "Detectors": [],
    "DetectorConfig": {}
  },
  "Sampler": "",
  "BuilderExtensions": []
}
```

## Configuration file setup

To get started, add the configuration structure to the root of your `appsettings.{Environment}.json` and follow the next sections covering settings and components. Empty sections are not required.

The environment variables `"OTEL_SERVICE_NAME", "OTEL_RESOURCE_ATTRIBUTES"` also need to be defined in order for useful telemetry to be emitted. You can set these in the root of your file for convenience but it is not recommended in deployed environments (as they need to be adjusted by environment). Use the template from [docs/README.md Getting Started](../README.md#with-a-new--existing-dotnet-app) if you haven't already set these.

## Enabling telemetry signal collection

OpenTelemetry signal collection is enabled via setting `SimpleOpenTelemetry:[Metric/Trace/Log]` sections as `{}`. Omitting any of these will not register the collection / exporting or that signals telemetry. As an example, if your cloud provider is already collecting logs you need you may opt omit the `"Log"` section.

Normally you will want to add other components for each signal, most importantly an exporter (like OTLP). These are covered in the [OpenTelemetry ](#opentelemetry-component-configuration)

Below covers information about each signal collection settings and documentation. See [App instrumentation tips](../README.md#app-instrumentation-tips) for guidance on what to set these to.

## Logging

### Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via `SimpleOpenTelemetry:Log:Settings`:

- IncludeFormattedMessage - bool (default: false)
- IncludeScopes - bool (default: false)
- ParseStateValues - bool (default: false)

For settings details see [github opentelemetry-dotnet OpenTelemetryLoggerOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Logs/ILogger/OpenTelemetryLoggerOptions.cs)

### Logging providers

Logging providers are not cleared by SimpleOpenTelemetry or OpenTelemetry, but one will be added if the `SimpleOpenTelemetry:Log` section is defined. If you wish to only use this logging provider and not the [default providers in a Generic host application](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0#logging-providers) run `builder.Logging.ClearProviders()` before AddSimpleOpenTelemetry() as you can see in the [localdev example apps](../../example-apps/localdev/).

## Metrics

### Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via `SimpleOpenTelemetry:Metric:Settings`:

- MetricLimit - int (default: 1000)

OpenTelemetry Documentation: [opentelemetry.io metrics best practices](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices)

### CustomMeters

Set via `SimpleOpenTelemetry:Metric:CustomMeters`, it is possible to consume other meters not setup by `SimpleOpenTelemetry:Metric:Instrumentation` components. Eg a custom metric you output from your app see [Instrumenting your apps](../README.md#metrics)

## Tracing

Tracing in OpenTelemetry dotnet sdk defaults to `parentbased_always_on` meaning 100% of traces are emitted. For production environments, due to the high cost, a sampling strategy should be in place either at the app side, collector side or both.

### Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via `SimpleOpenTelemetry:Trace:Settings`:

- SetErrorStatusOnException - bool (default: false)

OpenTelemetry Documentation: [opentelemetry.io traces reporting exceptions](https://opentelemetry.io/docs/languages/dotnet/traces/reporting-exceptions/)

### Sources

Set via `SimpleOpenTelemetry:Trace:Sources`, this sets up listening to other traces not listened to by the ``SimpleOpenTelemetry:Trace:Instrumentation` components. Eg. your app custom traces. See [Instrumenting your apps](../README.md#distributed-tracing) and the [aspnetcore example app](../../example-apps/localdev/aspnetcore/Controllers/HomeController.cs) for an example custom trace.

## OpenTelemetry component configuration

The next sections cover setting up the subsections of your "SimpleOpenTelemetry" config to enable components, with nuget package to add and snippets / samples. You will want at minimum an exporter to get started.

- [Distros](./distros.md)
- [Instrumentation](./instrumentations.md)
- [Exporters](./exporters.md)
- [Resource Detectors](./resource-detectors.md)
- [Propagators](./propagators.md)
- [Samplers](./samplers.md)
- [Extensions](./extensions.md)

## Configuration testing, debugging and deployment

For initial file setup and testing you can use a [example-apps/localdev/](../../example-apps/localdev/) with a local Grafana LGTM to view telemetry or use one of the [example-apps/cloud/](../../example-apps/cloud/) and connect a local Grafana LGTM or to your cloud provider endpoints. The cloud examples are setup with everything you need to deploy.

To verify / validate your configuration you can check the local Grafana instance and for warning / error events emitted from SimpleOpenTelemetry / OpenTelemetry by wiring up a listener see [SimpleOpenTelemetry Error Handling and Diagnostics](../README.md#simpleopentelemetry-error-handling-and-diagnostics)

## Configuration sources

SimpleOpenTelemetry uses dotnet's IConfiguration concepts and abstractions, it relies on the default configuration sources setup in generic host platforms to load in appsettings.json. Settings are loaded in a particular hierarchy noted in [MSLearn default app configuration sources](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#default-app-configuration-sources), meaning settings in the config file can be overridden via Env vars.

The configuration system also means you can also [add in other configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) before calling AddSimpleOpenTelemetry(). These are particularly useful for loading in sensitive values (keys, secrets etc).

As the IConfigurationProvider for environment variables is enabled by default, you can define all SimpleOpenTelemetry settings and OTEL* env vars in environment variables or in the appsettings.json file. You can also put the "SimpleOpenTelemetry" section and OTEL* settings in its own file instead of appsettings.json if you wish with some extra configuration before calling AddSimpleOpenTelemetry(). See https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#file-configuration-provider

For local development with sensitive values, it is recommended to take advantage of [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for configuration / env var values.

## Environment variables

When setting SimpleOpenTelemetry configuration as Environment variables use the \_\_ separator for the hierarchical structure eg SimpleOpenTelemetry:Trace:Options as SimpleOpenTelemetry\_\_Trace\_\_Options. See [MSLearn - configuration-keys-and-values](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#configuration-keys-and-values).

The OpenTelemetry OTEL\_\* environment variables / json config are partially supported (see details further below) and load in by default (as this is done by the underlying OpenTelemetry SDK registration) but for many components those settings can be defined explicitly for their signal type/functionality in the configuration file or in code using the OpenTelemetryBuilder returned from SimpleOpenTelemetry.

## OpenTelemetry environment variables

Below are required and some core OTEL\_ environment variables you can set in Env vars or appsettings.ENV.json value. OpenTelemetry automatically reads these in. The [Configuration file setup](#configuration-file-setup) contains the required ones, \* indicates a required setting:

- [OTEL_SERVICE_NAME](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)\*
- [OTEL_RESOURCE_ATTRIBUTES](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)\*
- [OTEL_EXPORTER_OTLP_PROTOCOL](https://opentelemetry.io/docs/specs/otel/protocol/exporter/) - default: 'gRPC' which is common for otel collector sidecar use
  [OTEL_EXPORTER_OTLP_ENDPOINT](https://opentelemetry.io/docs/specs/otel/protocol/exporter/) - default: 'http://localhost:4317' which is common for otel collector sidecar use
- [OTEL_TRACES_SAMPLER, OTEL_TRACES_SAMPLER_ARG](https://opentelemetry.io/docs/languages/dotnet/sampling/#environment-variable-configuration)
- [OTEL_METRICS_EXEMPLAR_FILTER](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#exemplar) contrary to spec, exemplars are 'always_off' by default due to performance costs concerns by the implementation team. See the [opentelemetry-dotnet doco](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/metrics/exemplars/README.md) for how to use.
- [OTEL_SDK_DISABLED](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)

<br>

⚠️ **IMPORTANT** ⚠️
<br/>

The OpenTelemetry Documentation [SDK Environment Variables](https://OpenTelemetry.io/docs/specs/otel/configuration/sdk-environment-variables) page is a specification not a reference for the dotnet implementation. Many of these are (as of july 2026) unsupported such as **OTEL_PROPAGATORS, OTEL_TRACES_EXPORTER, OTEL_LOGS_EXPORTER, OTEL_METRICS_EXPORTER**

> If you wish to make use of any of the environment variables in the spec but are not above, check the [dotnet documentation to confirm it is implemented](https://OpenTelemetry.io/docs/languages/dotnet/getting-started/), or even quicker too dive into the [OpenTelemetry-dotnet repo](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main) to search.

## Supported OpenTelemetry components

While all OpenTelemetry components in [OpenTelemetry-dotnet-contrib](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib) distros, and vendor implementations of components _could_ be loaded using SimpleOpenTelemetry's configuration syntax, these are gated through registered assembly sets in the below folders for security and to ensure those configurations have been tested with this library:

- [DistroAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Distro/DistroAssemblies.cs)
- [ExporterAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Exporter/ExporterAssemblies.cs)
- [Trace / Metric InstrumentationAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Instrumentation/InstrumentationAssemblies.cs)
- [ExtensionAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Extension/ExtensionAssemblies.cs)
- [SamplerAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Sampler/SamplerAssemblies.cs)
- [PropagatorAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Propagator/PropagatorAssemblies.cs)
- [ResourceDetectorAssemblies](../../src/SimpleOpenTelemetry/OtelComponents/Resource/ResourceDetectorAssemblies.cs)

If a component type (eg. Processors), extension or setting isn't available to configure you can load/configure it in code using the OpenTelemetryBuilder returned from `AddSimpleOpenTelemetry()`. To search for a component check the [OpenTelemetry Registry](https://OpenTelemetry.io/ecosystem/registry/).

If what you need isn't available there, you can build your own following the OpenTelemetry guidelines for extending [traces](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/trace/extending-the-sdk/README.md) [logs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/logs/extending-the-sdk/README.md) and [metrics](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/metrics/extending-the-sdk/README.md).

If there is a registry component you would like added, feel free to raise a PR, or [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new).
