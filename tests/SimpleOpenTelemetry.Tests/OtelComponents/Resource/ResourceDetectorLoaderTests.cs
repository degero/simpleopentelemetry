using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Resource;

[CollectionDefinition("ResourceDetectorLoaderTests", DisableParallelization = true)]
public class ResourceDetectorLoaderTestsCollection {}

[Collection("ResourceDetectorLoaderTests")]
public class ResourceDetectorLoaderTests : IDisposable
{
    private readonly TestEventListener _listener;
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static SimpleOpenTelemetryOptions BuildOptionsWithDetectors(params string[] detectorNames)
        => new SimpleOpenTelemetryOptions
        {
            Resource = new ResourceOptions
            {
                Detectors = detectorNames
            }
        };
    
    public ResourceDetectorLoaderTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Theory]
    [MemberData(nameof(GetAllResourceDetectors), true)]
    [MemberData(nameof(GetAllResourceDetectors), false)]
    public void AddResourceDetectors_WithKnownDetector_LogsSuccessOrFailure(
        ResourceDetectorEnum detector,
        string assemblyName,
        bool packageInstalled)
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }

        
        var loader = new ResourceDetectorLoader(_configuration, packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var options = BuildOptionsWithDetectors(detector.ToString());
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // ACT
        loader.AddResourceDetectors(resourceBuilder, options);

        // ASSERT
        var successEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                e.Payload.Any(p => p?.ToString()?.Contains($"Registered resource detector '{detector}'") ?? false));
        var errorEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Error &&
                e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register resource detector '{detector}'") ?? false) &&
                e.Payload.Any(p => p?.ToString()?.Contains("Ensure you have added the required nuget package to your project.") ?? false));

        if (packageInstalled || assemblyName == "SimpleOpenTelemetry")
        {
            Assert.NotNull(successEvent);
            Assert.Null(errorEvent);
        }
        else
        {
            Assert.NotNull(errorEvent);
            Assert.Null(successEvent);
        }
    }

    [Fact]
    public void AddResourceDetectors_WithUnsupportedDetector_LogsUnsupportedDetectorError()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);

        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var options = BuildOptionsWithDetectors("NonExistentDetector");
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // ACT
        loader.AddResourceDetectors(resourceBuilder, options);

        // ASSERT
        var errorEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Error &&
                e.Payload.Any(p => p?.ToString()?.Contains("Unsupported Resource Detector type 'NonExistentDetector'") ?? false));

        Assert.NotNull(errorEvent);
    }

    public static IEnumerable<object[]> GetAllResourceDetectors(bool packageInstalled)
    {
        foreach (var detector in ResourceDetectorAssemblies.KnownResourceDetectors)
        {
            if (!packageInstalled && detector.Value.AssemblyName == "SimpleOpenTelemetry")
            {
                continue;
            }

            yield return new object[] { detector.Key, detector.Value.AssemblyName, packageInstalled };
        }
    }
}
