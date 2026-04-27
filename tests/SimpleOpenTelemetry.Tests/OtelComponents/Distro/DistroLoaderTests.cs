using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Distro;

public class DistroLoaderTests
{
    private AssemblyExecution _assemblyExec = new AssemblyExecution(); 

    [Theory]
    [InlineData(nameof(DistroEnum.AzureMonitorAspNetCore), "Azure.Monitor.OpenTelemetry.AspNetCore", true)]
    [InlineData(nameof(DistroEnum.AzureMonitorAspNetCore), "Azure.Monitor.OpenTelemetry.AspNetCore", false)]
    public void LoadDistro_WithKnownDistrosInConfiguration_LogsSuccessWhenPackageInstalled_AndReturnsTrue(
        string distroName,
        string assemblyName,
        bool packageInstalled
    )
    {
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
            mockAssemblyExec.Setup(r => r.GetAssembly(assemblyName)).Throws(new Exception($"Cannot load assembly '{assemblyName}'. " +
                    $"Ensure you have added the required nuget package to your project."));

        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new DistroLoader(configuration, packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var options = new SimpleOpenTelemetryOptions
        {
            Distro = distroName
        };

        var hostBuilder = Host.CreateApplicationBuilder();
        var otelBuilder = hostBuilder.Services.AddOpenTelemetry();

        var foundConfig = sut.LoadDistro(otelBuilder, options);

        Assert.True(foundConfig);

        var successEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry distro '{options.Distro}'") ?? false));
        
        var errorEvent = listener.Events.FirstOrDefault(e =>
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
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new DistroLoader(configuration, _assemblyExec);
        var options = new SimpleOpenTelemetryOptions
        {
            Distro = "NotARealDistro"
        };

        var hostBuilder = Host.CreateApplicationBuilder();
        var otelBuilder = hostBuilder.Services.AddOpenTelemetry();

        var foundConfig = sut.LoadDistro(otelBuilder, options);

        Assert.True(foundConfig);

        var successEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry distro '{options.Distro}'") ?? false));
        
        var errorEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains( $"Unsupported OpenTelemetry Distro '{options.Distro}'. Please check your SimpleOpenTelemetry configuration.") ?? false));
 
        Assert.Null(successEvent);
        Assert.NotNull(errorEvent);
    }

    
    [Fact]
    public void LoadDistro_WithNoDistroSetting_ReturnsFalse()
    {
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new DistroLoader(configuration, _assemblyExec);
        var options = new SimpleOpenTelemetryOptions
        {
        };

        var hostBuilder = Host.CreateApplicationBuilder();
        var otelBuilder = hostBuilder.Services.AddOpenTelemetry();

        var foundConfig = sut.LoadDistro(otelBuilder, options);

        Assert.False(foundConfig);
    }
}
