namespace SimpleOpenTelemetry.Tests;

using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Extensions;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSimpleOpenTelemetry_RegistersTracerProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleOpenTelemetry(builder =>
        {
            builder.WithServiceName("test-service");
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tracerProvider = serviceProvider.GetRequiredService<OpenTelemetry.Trace.TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void AddSimpleOpenTelemetry_NullConfigureThrows()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddSimpleOpenTelemetry(null!));
    }
}
