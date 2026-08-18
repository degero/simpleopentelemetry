# Samplers Configuration

**IMPORTANT**: ⚠️ **It is recommended you install [these versions tested against SimpleOpenTelemetry](../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

Set Trace samplers in the configuration `SimpleOpenTelemetry:Trace:Sampler` string field.

The below allow vendor sampler configuration as an alternative to OpenTelemetry's [built-in samplers](https://OpenTelemetry.io/docs/specs/otel/trace/sdk/#built-in-samplers). Builtin samplers can be set in OTEL_TRACES_SAMPLER of the root json configuration or env var. Some requires values in OTEL_TRACES_SAMPLER_ARG. The sampler defaults to 'parentbased_always_on'.

For a full list of all the additional supported samplers see [SamplerEnum](./src/SimpleOpenTelemetry/Sampler/SamplerAssemblies.cs)

For Azure users, sampling is built into the exporter setup/options.

Available samplers are:

## AWS X-Ray Remote Sampler (Unsupported)

Stability: Alpha (as of July 2026)

Notes: Currently unsupported due to irregular registration pattern requiring prebuilt opentelemetry resource. See [example-apps/cloud/aws/ecs](../../example-apps/cloud/aws/ecs/README.md#x-ray-remote-sampling) for using this via code.

Documentation: [AWS X-Ray Remote Sampler](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Sampler.AWS/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Sampler.AWS --version x.x.x`

SimpleOpenTelemetry:Trace:Sampler json:

```json
"aws"
```

---
