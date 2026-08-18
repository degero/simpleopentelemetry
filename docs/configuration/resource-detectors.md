# Resource Detectors Configuration

**IMPORTANT**: ⚠️ **It is recommended you install [these versions tested against SimpleOpenTelemetry](../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

Set Resource detectors in the configuration `SimpleOpenTelemetry:Resource:Detectors[]` string array. These will process in the array order.

⚠️ _Detectors may override the resource attributes set by a preceding detector eg 'service.name' so it is recommended to read their documentation before adding. Some cloud platforms also have 'reserved' attributes injected such as AWS._ ⚠️

Options for resource detectors can be placed in `SimpleOpenTelemetry:Resource:DetectorConfig:<Type>:<OptionsField>`

eg `SimpleOpenTelemetry:Resource:DetectorConfig::AWS:SemanticConventionVersion = "V1_29_0"`

⚠️ _Complex types or Func<>/Action<>/etc aren't supported on Options fields._ ⚠️

For a list of supported resource detectors see [ResourceDetectorEnum.cs](../../src/SimpleOpenTelemetry/OtelComponents/Resource/ResourceDetectorEnum.cs)

Available resource detectors are:

## AssemblyVersion

Package Stability: Stable

Notes: Examines the 'built' assembly version that may be set in a CICD pipeline and in msbuild and assigns this to service.version resource attribute. Avoids the need to explicitly set service.version in config. eg set a dotnet build / publish parameter V-p:Version=<<MyVersion>>. This detector can be overridden by setting 'service.version' in OTEL_RESOURCE_ATTRIBUTES of appsettings.json / env vars.

Nuget Package: not needed (built into SimpleOpenTelemetry)

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"AssemblyVersion"
```

## Host

Package Stability: Beta (as of july 2026)

Documentation: [Resource Host Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Host/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Host --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"host"
```

## Container

Package Stability: Beta (as of july 2026)

Documentation: [Container Resource Detector README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Container/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Container --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"container"
```

## Operating System

Package Stability: Alpha (as of july 2026)

Documentation: [Operating System Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.OperatingSystem/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.OperatingSystem --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"os"
```

## Process

Package Stability: Beta (as of july 2026)

Documentation: [Process Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Process/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Process --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"process"
```

## Process Runtime

Package Stability: Beta (as of july 2026)

Documentation: [Process Runtime Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.ProcessRuntime/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.ProcessRuntime --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"processruntime"
```

## AWS

Package Stability: Stable

Documentation: [AWS Resource Detectors](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.AWS/README.md)

Options: optional, see [AWSResourceBuilderOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.AWS/AWSResourceBuilderOptions.cs) and [snippets/resourcedetectors/aws.json](./snippets/resourcedetectors/aws.json)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.AWS --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"aws"
```

## Azure

Package Stability: Beta (as of July 2026)

Documentation: [Resource Detectors for Azure cloud environments](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Azure/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Azure --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"azure"
```

Notes:

OTEL_SERVICENAME / service.name (and several OTEL_RESOURCE attributes), a core OTEL attribute will be overridden by the Azure's resource detector using the Azure resource's name and resource information. Information regarding which are set is in the above doco.

It is possible to change by code.

eg.

```csharp
var otelBuilder = builder.AddSimpleOpenTelemetry();
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
if (!string.IsNullOrEmpty(serviceName)) {
    otelBuilder.ConfigureResource(r => r.AddAttributes(new Dictionary<string, object>
    {
        ["service.name"] = serviceName
    }));
}
```

OR

_USE WITH CAUTION_

Add an `"envvar"` after this detector. This will 'rewrite' the attributes by taking values from OTEL_RESOURCE_ATTRIBUTES, OTEL_SERVICE_NAME. Refer to the detector doco information on attributes it sets and ensure they are not in OTEL_RESOURCE_ATTRIBUTES.

## Google Cloud Platform

Package Stability: Alpha (as of July 2026)

Documentation: [Resource Detectors for Google Cloud Platform](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Gcp/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Gcp --version x.x.x`

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"gcp"
```

## EnvVar

Package Stability: Stable

Notes: OpenTelemetry SDK adds this by default. Only use this if the SDK changes to not include it by default.

Documentation: [OpenTelemetry SDK ResourceBuilderExtensions.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/08df7481053204a5ba10c61bb4f1a21d5d3fcefa/src/OpenTelemetry/Resources/ResourceBuilderExtensions.cs#L124)

Nuget Package: not needed (Opentelemetry SDK)

SimpleOpenTelemetry:Resource:Detectors[] json:

```json
"EnvVar"
```

---
