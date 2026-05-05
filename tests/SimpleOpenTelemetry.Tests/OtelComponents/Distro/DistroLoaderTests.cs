using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Distro;

public class DistroLoaderTests : IDisposable
{
    private readonly TestEventListener _listener;

    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution(); 

    public DistroLoaderTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Theory]
    [InlineData(nameof(DistroEnum.AzureMonitorAspNetCore), "Azure.Monitor.OpenTelemetry.AspNetCore", true)]
    [InlineData(nameof(DistroEnum.AzureMonitorAspNetCore), "Azure.Monitor.OpenTelemetry.AspNetCore", false)]
    public void LoadDistro_WithKnownDistrosInConfiguration_LogsSuccessWhenPackageInstalled_AndReturnsTrue(
        string distroName,
        string assemblyName,
        bool packageInstalled
    )
    {
        // ARRANGE
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
            mockAssemblyExec.Setup(r => r.GetAssembly(assemblyName)).Throws(new Exception($"Cannot load assembly '{assemblyName}'. " +
                    $"Ensure you have added the required nuget package to your project."));

        
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var target = new DistroLoader(configuration, packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var options = new SimpleOpenTelemetryOptions
        {
            Distro = distroName
        };

        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();
        
        // ACT
        var foundConfig = target.LoadDistro(otelBuilder, options);

        // ASSERT
        Assert.True(foundConfig);

        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry distro '{options.Distro}'") ?? false));
        
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains("Ensure you have added the required nuget package to your project.") ?? false) && 
            e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register OpenTelemetry distro '{options.Distro}'") ?? false));

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
    public void LoadDistro_WithUnsupportedDistro_LogsNoRegistrationEvents_AndReturnsTrue()
    {
        // ARRANGE
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var target = new DistroLoader(configuration, _assemblyExec);
        var options = new SimpleOpenTelemetryOptions
        {
            Distro = "NotARealDistro"
        };

        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        // ACT
        var foundConfig = target.LoadDistro(otelBuilder, options);

        // ASSERT
        Assert.True(foundConfig);

        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry distro '{options.Distro}'") ?? false));
        
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains( $"Unsupported OpenTelemetry Distro '{options.Distro}'. Please check your SimpleOpenTelemetry configuration.") ?? false));
 
        Assert.Null(successEvent);
        Assert.NotNull(errorEvent);
    }

    
    [Fact]
    public void LoadDistro_WithNoDistroSetting_ReturnsFalse()
    {
        // ARRANGE
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var target = new DistroLoader(configuration, _assemblyExec);
        var options = new SimpleOpenTelemetryOptions();

        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        // ACT
        var foundConfig = target.LoadDistro(otelBuilder, options);
        
        // ASSERT
        Assert.False(foundConfig);
    }
}
