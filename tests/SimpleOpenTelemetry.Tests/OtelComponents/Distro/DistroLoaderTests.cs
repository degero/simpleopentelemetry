using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Distro;

[CollectionDefinition("DistroLoaderTests", DisableParallelization = true)]
public class DistroLoaderTestsCollection {}


[Collection("DistroLoaderTests")]
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
    [InlineData(nameof(DistroEnum.AzureMonitorAspNetCore), """
    {
        "SimpleOpenTelemetry:DistroOptions:ConnectionString": "InstrumentationKey=asdfasdf;",
        "SimpleOpenTelemetry:DistroOptions:Credential": "Azure.Identity.DefaultAzureCredential"
    }
    """)]
    public void LoadDistro_WithDistroOptions_LogsSuccess_AndReturnsTrue(
        string distroName,
        string? optionsJson = null
    )
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        var mockAssemblyExec = new Mock<AssemblyExecution>{ CallBase = true};
      
        var target = new DistroLoader(mockAssemblyExec.Object);
        var options = new SimpleOpenTelemetryOptions
        {
            Distro = distroName
        };

        if (optionsJson is not null)
        {
            var distroOptions = new ConfigurationBuilder()
            .AddInMemoryCollection(JsonSerializer.Deserialize<Dictionary<string, string?>>(optionsJson)!)
            .Build()
            .GetSection($"{SimpleOpenTelemetryOptions.SectionName}:DistroOptions");

            options.DistroOptions = distroOptions;
        }

        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();
        
        // ACT
        var foundConfig = target.LoadDistro(otelBuilder, options);

        // ASSERT
        Assert.True(foundConfig);

        // Assert action method with options action called
        mockAssemblyExec.Verify(r => r.InvokeWithAction(It.IsAny<MethodInfo>(), It.IsAny<object>(), 
            It.IsAny<IConfiguration>()),
            Times.Exactly(1));

        mockAssemblyExec.Verify(r => r.InvokeParameterless(It.IsAny<MethodInfo>(), It.IsAny<object>()),
            Times.Exactly(0));

        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Distro '{options.Distro}'") ?? false));
        
        var errorEvent = _listener.Events.Where(e =>
            e.Level == EventLevel.Error);

        Assert.NotNull(successEvent);
        Assert.Empty(errorEvent);
    }

    [Fact]
    public void LoadDistro_WithUnsupportedDistro_LogsNoRegistrationEvents_AndReturnsTrue()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        var target = new DistroLoader(_assemblyExec);
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
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Distro '{options.Distro}'") ?? false) ?? false));
        
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains( $"Unsupported OpenTelemetry Distro '{options.Distro}' for builder 'OpenTelemetryBuilder'. Please check your SimpleOpenTelemetry configuration.") ?? false) ?? false));
 
        Assert.Null(successEvent);
        Assert.NotNull(errorEvent);
    }

    
    [Fact]
    public void LoadDistro_WithNoDistroSetting_ReturnsFalse()
    {
        // ARRANGE
        var target = new DistroLoader(_assemblyExec);
        var options = new SimpleOpenTelemetryOptions();

        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        // ACT
        var foundConfig = target.LoadDistro(otelBuilder, options);
        
        // ASSERT
        Assert.False(foundConfig);
    }
}
