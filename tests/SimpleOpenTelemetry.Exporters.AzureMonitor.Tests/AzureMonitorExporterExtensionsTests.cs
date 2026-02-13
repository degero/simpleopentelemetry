namespace SimpleOpenTelemetry.Exporters.AzureMonitor.Tests;

using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Exporters.AzureMonitor.Extensions;
using Xunit;

public class AzureMonitorExporterExtensionsTests
{
    [Fact]
    public void WithAzureMonitorExporter_WithConnectionString_ReturnsBuilder()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();
        var connectionString = "InstrumentationKey=test-key";

        // Act
        var result = builder.WithAzureMonitorExporter(connectionString);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithAzureMonitorExporter_NullConnectionString_Throws()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithAzureMonitorExporter((string)null!));
    }

    [Fact]
    public void WithAzureMonitorExporter_EmptyConnectionString_Throws()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithAzureMonitorExporter(""));
    }

    [Fact]
    public void WithAzureMonitorExporter_WithConfiguration_ReturnsBuilder()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();
        var connectionString = "InstrumentationKey=test-key";

        // Act
        var result = builder.WithAzureMonitorExporter(connectionString, options =>
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
        var builder = new SimpleOpenTelemetryBuilder();

        // Act
        var result = builder.WithAzureMonitorExporter(options =>
        {
            // Custom configuration
        });

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }
}
