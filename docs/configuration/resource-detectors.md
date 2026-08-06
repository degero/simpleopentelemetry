
# Resource Detectors Configuration

Set Resource detectors in the configuration `SimpleOpenTelemetry:Resource:Detectors[]` string array. These will process in the array order.

**IMPORTANT**: ⚠️ *Detectors may override the resource attributes set by a preceding detector eg 'service.name' so it is recommended to read their documentation before adding. Some cloud platforms also have 'reserved' attributes injected such as AWS.* ⚠️

All the supported resource detectors are listed here [ResourceDetectorEnum](./src/SimpleOpenTelemetry/Resource/ResourceDetectorEnum.cs)

Available resource detectors are:


## AssemblyVersion

Stability: Stable

Notes: Examines the 'built' assembly version that may be set in a CICD pipeline and in msbuild and assigns this to service.version resource attribute. Avoids the need to explicitly set service.version in config. eg set a dotnet build / publish parameter V-p:Version=<<MyVersion>>

Nuget Package: not needed (built into SimpleOpenTelemetry)

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "AssemblyVersion"
 ```


## Host

Stability: Beta (as of july 2026)

Documentation: [Resource Host Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Host/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Host --prerelease --version 1.15.1-beta.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "host"
 ```


## Container

Stability: Beta (as of july 2026)

Documentation: [Container Resource Detector README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Container/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Container --prerelease --version 1.15.1-beta.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "container"
 ```


## Operating System

Stability: Alpha (as of july 2026)

Documentation: [Operating System Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.OperatingSystem/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.OperatingSystem --prerelease --version 1.15.1-beta.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "os"
 ```


## Process

Stability: Beta (as of july 2026)

Documentation: [Process Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Process/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Process --prerelease --version 1.15.1-beta.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "process"
 ```


## Process Runtime

Stability: Beta (as of july 2026)

Documentation: [Process Runtime Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.ProcessRuntime/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.ProcessRuntime --prerelease --version 1.15.1-beta.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "processruntime"
 ```


## AWS

*AWS*

Stability: Stable

Documentation: [AWS Resource Detectors](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.AWS/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.AWS --version 1.15.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "aws"
 ```

For supported configurable options see [snippets/resourcedetectors/aws.json](./snippets/resourcedetectors/aws.json)


## Azure

Stability: Beta (as of July 2026)

Documentation: [Resource Detectors for Azure cloud environments](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Azure/README.md)

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Resources.Azure --version 1.15.1-beta.1`

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

*USE WITH CAUTION*

Add an `"envvar"` after this detector. This will 'rewrite' the attributes by taking values from OTEL_RESOURCE_ATTRIBUTES, OTEL_SERVICE_NAME. Refer to the detector doco information on attributes it sets and ensure they are not in OTEL_RESOURCE_ATTRIBUTES.


## Google Cloud Platform

Stability: Alpha (as of July 2026)

Documentation: [Resource Detectors for Google Cloud Platform](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Gcp/README.md)

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Resources.Gcp --version 1.0.0-alpha.1`

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "gcp"
 ```


## EnvVar

Stability: Stable

Notes: OpenTelemetry SDK adds this by default. Only use this if the SDK changes to not include it by default.

Documentation: [OpenTelemetry SDK ResourceBuilderExtensions.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/08df7481053204a5ba10c61bb4f1a21d5d3fcefa/src/OpenTelemetry/Resources/ResourceBuilderExtensions.cs#L124)

Nuget Package: not needed (Opentelemetry SDK)

SimpleOpenTelemetry:Resource:Detectors[] json:

 ```json
 "EnvVar"
 ```


---

