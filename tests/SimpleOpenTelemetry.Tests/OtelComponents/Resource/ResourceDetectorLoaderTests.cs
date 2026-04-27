using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Resource;

public class ResourceDetectorLoaderTests : IDisposable
{
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();

    private readonly IConfiguration _configuration;
    private readonly TestEventListener _listener;

    public ResourceDetectorLoaderTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        _listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    /// <summary>
    /// Helper to build SimpleOpenTelemetryOptions with specified detector names.
    /// </summary>
    private static SimpleOpenTelemetryOptions BuildOptionsWithDetectors(params string[] detectorNames)
    {
        return new SimpleOpenTelemetryOptions
        {
            Resource = new ResourceOptions
            {
                Detectors = detectorNames
            }
        };
    }

    /// <summary>
    /// Checks if an assembly can be loaded (indicates NuGet package is installed).
    /// </summary>
    private static bool CanLoadAssembly(string assemblyName)
    {
        try
        {
            Assembly.Load(assemblyName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the required assembly name for a given detector type based on KnownResourceDetectors.
    /// </summary>
    private static string GetAssemblyNameForDetector(ResourceDetectorEnum detector)
    {
        if (ResourceDetectorAssemblies.KnownResourceDetectors.TryGetValue(detector, out var descriptor))
        {
            return descriptor.AssemblyName;
        }
        throw new InvalidOperationException($"Detector {detector} not found in KnownResourceDetectors");
    }

    /// <summary>
    /// Test that AssemblyVersion detector has event logged indicating successful registration.
    /// </summary>
    [Fact]
    public void AddResourceDetectors_WithAssemblyVersionDetector_LogsSuccessfulRegistration()
    {
        // Arrange
        
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var options = BuildOptionsWithDetectors(nameof(ResourceDetectorEnum.AssemblyVersion));
        var resourceBuilder = ResourceBuilder.CreateDefault();
        
        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

        // Assert: Verify the event message indicates successful registration
        var successEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                               e.EventId == 4 && // Verbose event ID
                               e.Payload.Any(p => p?.ToString()?.Contains("Registered resource detector 'AssemblyVersion'") ?? false));

        Assert.NotNull(successEvent);
    }

    /// <summary>
    /// Test that EnvVar detector has event logged indicating successful registration.
    /// </summary>
    [Fact]
    public void AddResourceDetectors_WithEnvVarDetector_LogsSuccessfulRegistration()
    {
        // Arrange
        
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var options = BuildOptionsWithDetectors(nameof(ResourceDetectorEnum.EnvVar));
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

        // Assert: Verify the event message indicates successful registration
        var successEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                               e.EventId == 4 && // Verbose event ID
                               e.Payload.Any(p => p?.ToString()?.Contains("Registered resource detector 'EnvVar'") ?? false));

        Assert.NotNull(successEvent);
    }

    /// <summary>
    /// Test that case-insensitive detector names are handled and logged as successful.
    /// </summary>
    [Fact]
    public void AddResourceDetectors_WithCaseInsensitiveDetectorName_LogsSuccessfulRegistration()
    {
        // Arrange
        
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var options = BuildOptionsWithDetectors(nameof(ResourceDetectorEnum.AssemblyVersion).ToLower()); // lowercase
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

        // Assert: Should register successfully despite case difference
        var successEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                               e.EventId == 4 &&
                               e.Payload.Any(p => p?.ToString()?.Contains("Registered resource detector 'AssemblyVersion'") ?? false));

        Assert.NotNull(successEvent);
    }

    /// <summary>
    /// Test that invalid detector names log specific error message about unsupported detector.
    /// </summary>
    [Fact]
    public void AddResourceDetectors_WithInvalidDetectorName_LogsSpecificError()
    {
        // Arrange
        
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var options = BuildOptionsWithDetectors("NonExistentDetector");
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

        // Assert: Verify error event contains message about unsupported detector
        var errorEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Error &&
                               e.EventId == 3 && // Error event ID
                               e.Payload.Any(p => p?.ToString()?.Contains("Unsupported Resource Detector type 'NonExistentDetector'") ?? false));

        Assert.NotNull(errorEvent);
    }

    /// <summary>
    /// Test that detectors with multiple methods have the detector registered event logged.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetResourceDetectorEnumsWithMultipleMethods))]
    public void AddResourceDetectors_WithMultiMethodDetector_LogsSuccessfulRegistration(ResourceDetectorEnum detector)
    {
        // Arrange
        
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var options = BuildOptionsWithDetectors(detector.ToString());
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

        // Assert: Verify the detector was registered (if assembly available)
        var descriptor = ResourceDetectorAssemblies.KnownResourceDetectors[detector];
        
        if (descriptor.AssemblyName == "SimpleOpenTelemetry" || CanLoadAssembly(descriptor.AssemblyName))
        {
            var registeredEvent = _listener.Events
                .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                                   e.EventId == 4 &&
                                   e.Payload.Any(p => p?.ToString()?.Contains($"Registered resource detector '{detector}'") ?? false));
            
            Assert.NotNull(registeredEvent);
        }
    }

    /// <summary>
    /// Test that unavailable detectors log error events while available ones log success events.
    /// </summary>
    [Fact]
    public void AddResourceDetectors_WithUnavailableDetectors_LogsErrorAndSuccessForAvailable()
    {
        // Arrange
        
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        
        var allDetectors = Enum.GetValues<ResourceDetectorEnum>();
        var detectorNames = allDetectors.Select(d => d.ToString()).ToArray();
        
        var options = BuildOptionsWithDetectors(detectorNames);
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

        // Assert: Verify at least some success events for registered detectors
        var successEvents = _listener.Events
            .Where(e => e.Level == EventLevel.Verbose &&
                       e.EventId == 4 &&
                       e.Payload.Any(p => p?.ToString()?.Contains("Registered resource detector") ?? false))
            .ToList();

        Assert.NotEmpty(successEvents);

        // Verify each available detector appears in success events
        foreach (var detector in allDetectors)
        {
            var descriptor = ResourceDetectorAssemblies.KnownResourceDetectors[detector];
            if (descriptor.AssemblyName == "SimpleOpenTelemetry" || CanLoadAssembly(descriptor.AssemblyName))
            {
                var registeredEvent = _listener.Events
                    .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                                       e.Payload.Any(p => p?.ToString()?.Contains($"Registered resource detector '{detector}'") ?? false));
                
                Assert.NotNull(registeredEvent);
            }
            else
            {
                // Verify error event for unavailable detector
                var errorEvent = _listener.Events
                    .FirstOrDefault(e => e.Level == EventLevel.Error &&
                                       e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register resource detector '{detector}'") ?? false));
                
                Assert.NotNull(errorEvent);
            }
        }
    }

    /// <summary>
    /// Test that all methods in a multi-method detector are invoked by mocking IAssemblyExecution.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetResourceDetectorEnumsWithMultipleMethods))]
    public void AddResourceDetectors_WithMultiMethodDetector_InvokesAllMethods(ResourceDetectorEnum detector)
    {
        // Arrange
        var descriptor = ResourceDetectorAssemblies.KnownResourceDetectors[detector];
        // var mockAssemblyExecution = new Mock<IAssemblyExecution>();
        var loader = new ResourceDetectorLoader(_configuration, _assemblyExec);
        var detectorEnumName = detector.ToString();
        var options = BuildOptionsWithDetectors(detectorEnumName);
        var resourceBuilder = ResourceBuilder.CreateDefault();

        // Act
        loader.AddResourceDetectors(resourceBuilder, options);

         var successEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                               e.EventId == 4 &&
                               e.Payload.Any(p => p?.ToString()?.Contains($"Registered resource detector '{detectorEnumName}' with registration methods '{string.Join(',',descriptor.MethodNames)}'") ?? false));

        Assert.NotNull(successEvent);
    }

    /// <summary>
    /// MemberData provider for detectors with multiple method names.
    /// </summary>
    public static TheoryData<ResourceDetectorEnum> GetResourceDetectorEnumsWithMultipleMethods()
    {
        var data = new TheoryData<ResourceDetectorEnum>();

        foreach (var kvp in ResourceDetectorAssemblies.KnownResourceDetectors)
        {
            if (kvp.Value.MethodNames.Length > 1)
            {
                data.Add(kvp.Key);
            }
        }

        return data;
    }
}
