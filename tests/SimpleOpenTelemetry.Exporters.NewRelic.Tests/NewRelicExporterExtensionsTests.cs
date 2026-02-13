namespace SimpleOpenTelemetry.Exporters.NewRelic.Tests;

using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Exporters.NewRelic.Extensions;
using Xunit;

public class NewRelicExporterExtensionsTests
{
    [Fact]
    public void WithNewRelicExporter_WithApiKey_ReturnsBuilder()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();
        var apiKey = "test-api-key";

        // Act
        var result = builder.WithNewRelicExporter(apiKey, endpoint: null);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithNewRelicExporter_NullApiKey_Throws()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithNewRelicExporter(apiKey: null!));
    }

    [Fact]
    public void WithNewRelicExporter_EmptyApiKey_Throws()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithNewRelicExporter(apiKey: ""));
    }

    [Fact]
    public void WithNewRelicExporter_WithCustomEndpoint_ReturnsBuilder()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();
        var apiKey = "test-api-key";
        var endpoint = "https://otlp.custom.com:4317";

        // Act
        var result = builder.WithNewRelicExporter(apiKey, endpoint);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithNewRelicExporterEU_WithApiKey_ReturnsBuilder()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();
        var apiKey = "test-api-key";

        // Act
        var result = builder.WithNewRelicExporterEU(apiKey);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithNewRelicExporter_WithConfiguration_ReturnsBuilder()
    {
        // Arrange
        var builder = new SimpleOpenTelemetryBuilder();
        var apiKey = "test-api-key";

        // Act
        var result = builder.WithNewRelicExporter(apiKey, endpoint: null, configure: options =>
        {
            // Custom configuration
        });

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }
}
