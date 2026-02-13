namespace SimpleOpenTelemetry.Tests;

using SimpleOpenTelemetry.Builder;
using Xunit;

public class SimpleOpenTelemetryBuilderTests
{
    [Fact]
    public void CreateBuilder_ReturnsNonNull()
    {
        // Act
        var builder = RegisterOpenTelemetry.CreateBuilder();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithServiceName_SetServiceName()
    {
        // Arrange
        var builder = RegisterOpenTelemetry.CreateBuilder();

        // Act
        var result = builder.WithServiceName("test-service");

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithServiceName_NullThrows()
    {
        // Arrange
        var builder = RegisterOpenTelemetry.CreateBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithServiceName(null!));
    }

    [Fact]
    public void WithServiceName_EmptyThrows()
    {
        // Arrange
        var builder = RegisterOpenTelemetry.CreateBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithServiceName(""));
    }

    [Fact]
    public void WithServiceVersion_SetServiceVersion()
    {
        // Arrange
        var builder = RegisterOpenTelemetry.CreateBuilder();

        // Act
        var result = builder.WithServiceVersion("1.0.0");

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISimpleOpenTelemetryBuilder>(result);
    }

    [Fact]
    public void WithServiceVersion_NullThrows()
    {
        // Arrange
        var builder = RegisterOpenTelemetry.CreateBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithServiceVersion(null!));
    }

    [Fact]
    public void Build_ReturnsTracerProvider()
    {
        // Arrange
        var builder = RegisterOpenTelemetry.CreateBuilder()
            .WithServiceName("test-service")
            .WithServiceVersion("1.0.0");

        // Act
        var tracerProvider = builder.Build();

        // Assert
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void FluentChaining_Works()
    {
        // Arrange & Act
        var tracerProvider = RegisterOpenTelemetry.CreateBuilder()
            .WithServiceName("test-service")
            .WithServiceVersion("1.0.0")
            .Build();

        // Assert
        Assert.NotNull(tracerProvider);
    }
}
