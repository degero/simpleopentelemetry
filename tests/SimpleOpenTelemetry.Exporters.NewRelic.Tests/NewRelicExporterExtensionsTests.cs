namespace SimpleOpenTelemetry.Exporters.NewRelic.Tests;

using Moq;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Exporters.NewRelic.Extensions;
using Xunit;

public class NewRelicExporterExtensionsTests
{
    private SimpleOpenTelemetryBuilder target;
    public NewRelicExporterExtensionsTests()
    {
        target = new SimpleOpenTelemetryBuilder(new Mock<OpenTelemetryBuilder>().Object);
    }

    [Fact]
    public void WithNewRelicExporter_WithApiKey_ReturnsBuilder()
    {
        // Arrange
        
        var apiKey = "test-api-key";

        // Act
        var result = target.WithNewRelicExporter(apiKey, endpoint: null);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithNewRelicExporter_NullApiKey_Throws()
    {
        // Arrange
        

        // Act & Assert
        Assert.Throws<ArgumentException>(() => target.WithNewRelicExporter(apiKey: null!));
    }

    [Fact]
    public void WithNewRelicExporter_EmptyApiKey_Throws()
    {
        // Arrange
        

        // Act & Assert
        Assert.Throws<ArgumentException>(() => target.WithNewRelicExporter(apiKey: ""));
    }

    [Fact]
    public void WithNewRelicExporter_WithCustomEndpoint_ReturnsBuilder()
    {
        // Arrange
        
        var apiKey = "test-api-key";
        var endpoint = "https://otlp.custom.com:4317";

        // Act
        var result = target.WithNewRelicExporter(apiKey, endpoint);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithNewRelicExporter_WithConfiguration_ReturnsBuilder()
    {
        // Arrange
        
        var apiKey = "test-api-key";

        // Act
        var result = target.WithNewRelicExporter(apiKey, endpoint: null, configure: options =>
        {
            // Custom configuration
        });

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }
}
