# Propagators Configuration

**IMPORTANT**: ⚠️ **It is recommended you install [these versions tested against SimpleOpenTelemetry](../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

Set trace propagators in the configuration `SimpleOpenTelemetry:Trace:Propagators[]` json array. Multiple propagators can be specified.

⚠️ _The OpenTelemetry env var OTEL_PROPAGATORS is not supported (as of July 2026) in the OpenTelemetry dotnet sdk implementation_ ⚠️

**Nuget Packages**

OpenTelemetry has builtin default [SDK propagators](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main/src/OpenTelemetry.Api/Context/Propagation) so don't require adding a nupkg. To use the B3 propagator you will need to add the core sdk extensions nupkg: `dotnet add package OpenTelemetry.Extensions.Propagators --version x.x.x`

**Available Propagators in SimpleOpenTelemetry**

For a full list of all the supported propagators see [PropagatorEnum](./src/SimpleOpenTelemetry/Propagator/PropagatorAssemblies.cs)

Available propagators are:

## Default

OpenTelemetry initialisation defaults to use a 'CompositeTextMapPropagator' of BaggagePropagator (spec: 'baggage') and TraceContextPropagator (spec:'tracestate','traceparent'). By setting as Propagators as `null` or `[]` this will use the default.

The equivalent config setting (if you wish to append more to the default) being:

```json
"Propagators": ["tracecontext", "baggage"]
```

## Disable

If you wish to disable this, explicitly set SimpleOpenTelemetry:Trace:Propagators[] as:

```json
"none"
```

## AWS X-Ray Id Propagator

Stability: Stable

Documentation: [AWS X-Ray Id Propagator](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Extensions.AWS --version x.x.x`

SimpleOpenTelemetry:Trace:Propagators[] json:

```json
"awsxray"
```

---
