using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenTelemetry.Resources;
using OpenTelemetry.Resources.AWS;
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
    [InlineData(true)]
    [InlineData(false)]
    public void AddResourceDetectors_WithAWSDetector_PassesConfiguration_ToAddDetector_IfSet(
        bool setConfig
    )
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        mockAssemblyExec.Setup(r => r.GetAssembly(It.IsAny<string>()))
            .Returns((string input) =>
            {
                return _assemblyExec.GetAssembly(input);
            });
        mockAssemblyExec.Setup(r => r.FindParameterlessMethodWithAllDefaultValues(It.IsAny<Type>(), It.IsAny<Type>(),It.IsAny<string>()))
            .Returns((Type t1, Type t2, string input) =>
            {
                return _assemblyExec.FindParameterlessMethodWithAllDefaultValues(t1, t2, input);
            });
        mockAssemblyExec.Setup(r => r.FindActionOverload(It.IsAny<Type>(), It.IsAny<Type>(),It.IsAny<string>()))
            .Returns((Type t1, Type t2, string input) =>
            {
                return _assemblyExec.FindActionOverload(t1, t2, input);
            });
        mockAssemblyExec.Setup(r => r.InvokeWithAction(It.IsAny<MethodInfo>(), It.IsAny<object>(),
             It.IsAny<IConfiguration>())) .Returns((MethodInfo m1, object t2, IConfiguration config) =>
            {
                return _assemblyExec.InvokeWithAction(m1, t2, config);
            }).Verifiable();

         mockAssemblyExec.Setup(r => r.InvokeParameterlessOrDefaultedParameters(
            It.IsAny<MethodInfo>(), It.IsAny<Type>(), It.IsAny<object>()))
            .Returns((MethodInfo m1, Type t2,  object target) =>
            {
                return _assemblyExec.InvokeParameterlessOrDefaultedParameters(m1, t2, target);
            }).Verifiable();

        var opt = new AWSResourceBuilderOptions()
        {
            SemanticConventionVersion = SemanticConventionVersion.V1_28_0
        };

        var configSec = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()
        {
            [$"DetectorConfig:{ResourceDetectorEnum.AWS}:SemanticConventionVersion"] = SemanticConventionVersion.V1_28_0.ToString()
        }).Build();

        var loader = new ResourceDetectorLoader(mockAssemblyExec.Object);
        var resourceBuilder = ResourceBuilder.CreateDefault();
        var options = new SimpleOpenTelemetryOptions()
        {
            Resource = new()
            {
                Detectors = [ResourceDetectorEnum.AWS.ToString()],
            }
        };
        if (setConfig is true)
        {
           options.Resource.DetectorConfig = configSec.GetSection("DetectorConfig");
        }

        // ACT
        loader.AddResourceDetectors(resourceBuilder, options);

        // ASSERT
        if (setConfig)
        {
            mockAssemblyExec.Verify(r => r.InvokeWithAction(It.IsAny<MethodInfo>(), It.IsAny<object>(),
                It.Is<IConfiguration>(x => x.GetValue<string>("SemanticConventionVersion") == SemanticConventionVersion.V1_28_0.ToString())), Times.Exactly(4));
        }
        else
        {
            mockAssemblyExec.Verify(r => r.InvokeParameterlessOrDefaultedParameters(
                It.IsAny<MethodInfo>(), It.IsAny<Type>(), It.IsAny<object>()), Times.Exactly(4));
        }

        var successEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                e.Payload.Any(p => p?.ToString()?.Contains($"Registered resource detector '{ResourceDetectorEnum.AWS}'") ?? false));
        Assert.NotNull(successEvent);
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

        
        var loader = new ResourceDetectorLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
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

        var loader = new ResourceDetectorLoader(_assemblyExec);
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
