using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.OtelComponents.Extension;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.Reflection;
using Xunit;
using Xunit.Sdk;

namespace SimpleOpenTelemetryTests.OtelComponents.Extension;

[CollectionDefinition("ExtensionLoaderTests", DisableParallelization = true)]
public class ExtensionLoaderTestsCollection {}


[Collection("ExtensionLoaderTests")]
public class ExtensionLoaderTests: IDisposable
{
    private readonly TestEventListener _listener;
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

    public ExtensionLoaderTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Theory]
    [MemberData(nameof(GetAllTraceExtensions), true)]
    [MemberData(nameof(GetAllTraceExtensions), false)]
    public void AddTraceExtension_WithKnownTraceExtension_LogsSuccessOrFailure(
        string extension,
        string assemblyName,
        bool packageInstalled)
    {
        // ARRANGE
        _listener.ClearEvents();
        Assert.Empty(_listener.Events);

        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }
        
        var target = new ExtensionLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var services = new ServiceCollection();
        
        // ACT
        services.AddOpenTelemetry().WithTracing(t =>
        {
            target.AddTraceExtensions(t, new SimpleOpenTelemetryTraceOptions
            {
                Extensions = [ extension ]
            });
        });

        // ASSERT
        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Extension '{extension}'") ?? false) ?? false));
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Failed to register OpenTelemetry Extension '{extension}'") ?? false) ?? false) &&
            (e.Payload?.Any(p => p?.ToString()?.Contains("Ensure you have added the required nuget package to your project.") ?? false) ?? false));

        if (packageInstalled)
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
    public void AddMetricExtension_WithNoneEnum_LogsMissingDescriptorError()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        
        var target = new ExtensionLoader(_assemblyExec);
        var services = new ServiceCollection();

        // ACT
        services.AddOpenTelemetry().WithMetrics(m =>
        {
            target.AddMetricExtensions(m, new SimpleOpenTelemetryMetricOptions
            {
                Extensions = [ MetricExtensionsEnum.None.ToString() ]
            });
        });

        // ASSERT 
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload != null && 
            e.Payload.Any(p => p?.ToString()?.Contains("OpenTelemetry Extension MetricExtensionsEnum type 'None' for builder 'MeterProviderBuilder' not found to initialise.") ?? false));

        Assert.NotNull(errorEvent);
    }

    [Fact]
    public void AddLogExtension_WithNoneEnum_LogsMissingDescriptorError()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        
        var target = new ExtensionLoader(_assemblyExec);
        var services = new ServiceCollection();

        // ACT
        services.AddOpenTelemetry().WithLogging(l =>
        {
            target.AddLogExtensions(l, new SimpleOpenTelemetryLogOptions
            {
                Extensions = [ LogExtensionsEnum.None.ToString() ]
            });
        });

        // ASSERT
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains("OpenTelemetry Extension LogExtensionsEnum type 'None' for builder 'LoggerProviderBuilder' not found to initialise.") ?? false) ?? false));

        Assert.NotNull(errorEvent);
    }

    
    [Theory]
    [MemberData(nameof(GetAllBuilderExtensions), true)]
    [MemberData(nameof(GetAllBuilderExtensions), false)]
    public void AddBuilderExtension_WithlBuilderExtension_LogsSuccessOrFailure(
        string extension,
        string assemblyName,
        bool packageInstalled)
    {
        // ARRANGE
        _listener.ClearEvents();
        Assert.Empty(_listener.Events);

        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }
        
        var target = new ExtensionLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var services = new ServiceCollection();
        
        // ACT
        target.AddBuilderExtensions(services.AddOpenTelemetry(), new ()
        {
            BuilderExtensions = [
                new() { Type = extension }
            ]
        });

        // ASSERT
        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Extension '{extension}'") ?? false) ?? false));
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Failed to register OpenTelemetry Extension '{extension}'") ?? false) ?? false) &&
            (e.Payload?.Any(p => p?.ToString()?.Contains("Ensure you have added the required nuget package to your project.") ?? false) ?? false));

        if (packageInstalled)
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
    public void AddBuilderExtension_WithBuilderExtension_Azure_AndOptions_LogsSuccess()
    {
        // ARRANGE
        _listener.ClearEvents();
        Assert.Empty(_listener.Events);

      
        var target = new ExtensionLoader(_assemblyExec);
        var services = new ServiceCollection();
        
        var prefixedDict = new Dictionary<string, string?>()
        {
            ["BuilderExtensions:0:Options:ConnectionString"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            ["BuilderExtensions:0:Options:Credential"] = "Azure.Identity.DefaultAzureCredential"
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(prefixedDict).Build();
        var config = configuration.GetSection("BuilderExtensions:0:Options");
        var extension = BuilderExtensionsEnum.AzureMonitorExporter.ToString();

        // ACT
        target.AddBuilderExtensions(services.AddOpenTelemetry(), new ()
        {
            BuilderExtensions = [
                new() { Type = extension, Options = config}
            ]
        });

        // ASSERT
        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Extension '{extension}'") ?? false) ?? false));
        var errorEvent = _listener.Events.Where(e =>
            e.Level == EventLevel.Error);

        Assert.NotNull(successEvent);
        Assert.Empty(errorEvent);
    }

    public static IEnumerable<object[]> GetAllTraceExtensions(bool packageInstalled)
    {
        foreach (var extension in ExtensionAssemblies.KnownTraceExtensions)
        {
            yield return new object[] { extension.Key.ToString(), extension.Value.AssemblyName, packageInstalled };
        }
    }

    public static IEnumerable<object[]> GetAllBuilderExtensions(bool packageInstalled)
    {
        foreach (var extension in ExtensionAssemblies.KnownBuilderExtensions)
        {
            yield return new object[] { extension.Key.ToString(), extension.Value.AssemblyName, packageInstalled };
        }
    }
}
