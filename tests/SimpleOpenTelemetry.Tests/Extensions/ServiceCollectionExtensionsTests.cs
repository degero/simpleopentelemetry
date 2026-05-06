using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Extensions;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

[Collection("ServiceCollectionExtensionsTests")]
public class ServiceCollectionExtensionsTests
{
    private readonly TestEventListener _simpleOpenTelemetryEventListener;
    
    public ServiceCollectionExtensionsTests()
    {
        _simpleOpenTelemetryEventListener = new();
    }

    public void Dispose()
    {
        _simpleOpenTelemetryEventListener.Dispose();
    }

    [Fact]
    public void AddSimpleOpenTelemetry_LogsCriticalErrorEvent_AndReturns_OnNullServicesParameter()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        IServiceCollection? services = null;

        // Act
        ServiceCollectionExtensions.AddSimpleOpenTelemetry(services!, config);

        // ASSERT
        var failEvent = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Payload is not null && e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"IServiceCollection services parameter is null") ?? false));

        Assert.NotNull(failEvent);
    }
    
    [Fact]
    public void AddSimpleOpenTelemetry_LogsCriticalErrorEvent_AndReturns_OnNullConfigurationParameter()
    {
        // Arrange
        IConfiguration? config = null;
        var services = new ServiceCollection();

        // Act
        services.AddSimpleOpenTelemetry(config!);

        // ASSERT
        var failEvent = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Payload is not null && e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"IConfiguration configuration parameter is null") ?? false));

        Assert.NotNull(failEvent);
    }

    // TODO chad below tests these should just log Event not throw
    [Fact]
    public void AddSimpleOpenTelemetry_LogsCriticalErrorEvent_AndReturns_SimpleOpenTelemetrySectionIsMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();

        // ACT
        services.AddSimpleOpenTelemetry(config);

        // ASSERT
        var failEvent = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Payload is not null && e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"No configuration section 'SimpleOpenTelemetry'") ?? false));

        Assert.NotNull(failEvent);
    }

    [Fact]
    public void AddSimpleOpenTelemetry_LogsCriticalErrorEvent_AndReturns_When_SimpleOpenTelemetrySectionIsMissingAtLeastOneSignalConfig()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:ExporterOptions"] = "{}"
            })
            .Build();
        var services = new ServiceCollection();

         // ACT
        services.AddSimpleOpenTelemetry(config);

        // ASSERT
        var failEvent = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Payload is not null && e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Missing signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.") ?? false));

        Assert.NotNull(failEvent);
    }

    [Fact]
    public void AddSimpleOpenTelemetry_CallsAddOpenTelemetry_And_AddSimpleOpenTelemetryBuilder_Configure_When_ConfigurationExists()
    {
        var originalPropagator = Propagators.DefaultTextMapPropagator;
        try
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators:0"] = "B3",
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators:1"] = "Baggage"
                })
                .Build();

            var services = new ServiceCollection();

            // Act
            services.AddSimpleOpenTelemetry(config);
            
            // Assert - not ideal but cant verify by mocked / injected services due to extension method calling 
            // AddOpenTelemetry extension method and creating a new SimpleOpenTelemetryBuilder
            Assert.Contains(services, sd => sd.ServiceType == typeof(OpenTelemetry.Trace.TracerProvider)); // verify addOpenTelemetry
            var propagator = Propagators.DefaultTextMapPropagator;
            Assert.IsType<CompositeTextMapPropagator>(propagator);
            var innerPropagators = TestHelpers.GetCompositePropagators(propagator as CompositeTextMapPropagator).ToList();
            Assert.Equal(2, innerPropagators.Count);
            Assert.IsType<OpenTelemetry.Extensions.Propagators.B3Propagator>(innerPropagators[0]);
            Assert.IsType<BaggagePropagator>(innerPropagators[1]);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(originalPropagator);
        }
    }
}