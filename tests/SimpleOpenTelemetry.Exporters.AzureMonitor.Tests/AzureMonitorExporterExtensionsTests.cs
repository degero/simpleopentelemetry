namespace SimpleOpenTelemetry.Exporters.AzureMonitor.Tests;

using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Exporters.AzureMonitor.Extensions;
using Xunit;

public class AzureMonitorExporterExtensionsTests
{
    private SimpleOpenTelemetryBuilder target; 

    public AzureMonitorExporterExtensionsTests()
    {
        target = new SimpleOpenTelemetryBuilder(new Mock<OpenTelemetryBuilder>().Object);
    }

    [Fact]
    public void WithAzureMonitorExporter_WithConnectionString_ReturnsBuilder()
    {
        // Arrange
        
        var connectionString = "InstrumentationKey=test-key";

        // Act
        var result = target.WithAzureMonitorExporter(connectionString);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithAzureMonitorExporter_NullConnectionString_Throws()
    {
        // Arrange
        

        // Act & Assert
        Assert.Throws<ArgumentException>(() => target.WithAzureMonitorExporter((string)null!));
    }

    [Fact]
    public void WithAzureMonitorExporter_EmptyConnectionString_Throws()
    {
        // Arrange
        

        // Act & Assert
        Assert.Throws<ArgumentException>(() => target.WithAzureMonitorExporter(""));
    }

    [Fact]
    public void WithAzureMonitorExporter_WithConfiguration_ReturnsBuilder()
    {
        // Arrange
        
        var connectionString = "InstrumentationKey=test-key";

        // Act
        var result = target.WithAzureMonitorExporter(connectionString, options =>
        {
            // Custom configuration
        });

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithAzureMonitorExporter_WithAction_ReturnsBuilder()
    {
        // Arrange
        
        // Act
        var result = target.WithAzureMonitorExporter(options =>
        {
            // Custom configuration
        });

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }
}
