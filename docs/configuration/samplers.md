# Samplers Configuration

⚠️ **IMPORTANT** **It is recommended you install [package versions tested against SimpleOpenTelemetry](../otel-component-versions.md) referenced below.** ⚠️

<br/>

Set a trace sampler in the configuration string field:

```json
 "SimpleOpenTelemetry": {
    "Sampler": ""
 }
```

The below allow vendor sampler configuration as an alternative to OpenTelemetry's [built-in samplers](https://OpenTelemetry.io/docs/specs/otel/trace/sdk/#built-in-samplers). Builtin samplers can only be set in `OTEL_TRACES_SAMPLER` of the root json configuration or env var. Some require values in `OTEL_TRACES_SAMPLER_ARG` setting. The sampler used in SimpleOpenTelemetry defaults to `'parentbased_always_on'` from OpenTelemetry's default.

Available samplers are:

- [AWS Xray Remote Sampler (Unsupported)](#aws-x-ray-remote-sampler-unsupported)

## AWS X-Ray Remote Sampler (Unsupported)

**Package Stability**: Alpha (as of July 2026)

**Nuget Package**: `dotnet add package OpenTelemetry.Sampler.AWS --version x.x.x`

SimpleOpenTelemetry:Trace:Sampler json:

```json
"aws"
```

**Documentation**: [AWS X-Ray Remote Sampler](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Sampler.AWS/README.md)

**Notes**: Currently unsupported due to irregular registration pattern requiring prebuilt opentelemetry resource. See [example-apps/cloud/aws/ecs](../../example-apps/cloud/aws/ecs/README.md#x-ray-remote-sampling) for using this via code.

---
