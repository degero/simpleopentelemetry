# Propagators Configuration

⚠️ **IMPORTANT** **It is recommended you install [package versions tested against SimpleOpenTelemetry](../otel-component-versions.md) referenced below.** ⚠️

<br/>

Set trace propagators in the configuration string array (multiple can be specified):

```json
 "SimpleOpenTelemetry": {
    "Trace": {
      "Propagators": []
    }
 }
```

⚠️ _The OpenTelemetry spec env var OTEL_PROPAGATORS is not supported (as of July 2026) in OpenTelemetry dotnet_ ⚠️

**Available built-in OpenTelemetry Propagators**

OpenTelemetry has builtin default [SDK propagators](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main/src/OpenTelemetry.Api/Context/Propagation) that can be set in the array and don't require adding a nupkg. To use the B3 propagator you will need to add the core sdk extensions nupkg: `dotnet add package OpenTelemetry.Extensions.Propagators --version x.x.x`

**Available additional Propagators in SimpleOpenTelemetry**

Available additional propagators are:

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

**Package Stability**: Stable

**Nuget Package**: `dotnet add package OpenTelemetry.Extensions.AWS --version x.x.x`

SimpleOpenTelemetry:Trace:Propagators[] json:

```json
"awsxray"
```

**Documentation**: [AWS X-Ray Id Propagator](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)

---
