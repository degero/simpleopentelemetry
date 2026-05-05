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
    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsOnNullServices()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        IServiceCollection? services = null;

        // Act/Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddSimpleOpenTelemetry(services!, config));
    }
    
    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsOnNullConfiguration()
    {
        // Arrange
        IConfigurationRoot? config = null;
        var services = new ServiceCollection();
        
        // Act/Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddSimpleOpenTelemetry(services!, config));
    }

    // TODO chad below tests these should just log Event not throw
    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsWhenSimpleOpenTelemetrySectionIsMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();

        // Act/Assert
        var exception = Assert.Throws<Exception>(() => services.AddSimpleOpenTelemetry(config));
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsWhenSimpleOpenTelemetrySectionIsMissingAtLeastOneSignalConfig()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:ExporterOptions"] = "{}"
            })
            .Build();
        var services = new ServiceCollection();

        // Act/Assert
        var exception = Assert.Throws<Exception>(() => services.AddSimpleOpenTelemetry(config));
        var messageExpected = $"Signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.";
        Assert.Contains(messageExpected, exception.Message);
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