using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Extensions;
using System.IO;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

public class SimpleOpenTelemetryBuilderTests
{
    [Fact]
    public void Configure_ThrowsWhenSimpleOpenTelemetryConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);

        var exception = Assert.Throws<Exception>(() => builder.AddSimpleOpenTelemetry());
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void Configure_ThrowsWhenSimpleOpenTelemetryConfigSectionIsMissing_ForMetrics()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);

        var exception = Assert.Throws<Exception>(() => builder.AddSimpleOpenTelemetry());
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void Configure_ThrowsWhenSimpleOpenTelemetryConfigSectionIsMissing_ForLogging()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        // Note: HostApplicationBuilder automatically provides LoggerProvider via AddLogging
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);

        var exception = Assert.Throws<Exception>(() => builder.AddSimpleOpenTelemetry());
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void Configure_SetsUpTracing_WhenTraceExportersAreConfigured()
    {
        // Arrange: Configuration with trace exporters (using OTLP which is built-in)
        var jsonConfig = @"
        {
          ""SimpleOpenTelemetry"": {
            ""Trace"": {
              ""Exporters"": [
                { ""type"": ""otlp"" }
              ]
            }
          }
        }";

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        // Assert: TracerProvider should be registered when Trace exporters are configured
        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

  [Theory]
  [InlineData(@"""Settings"": { ""IncludeFormattedMessage"": true, ""IncludeScopes"": true, ""ParseStateValues"": true }", true, true, true)]
  [InlineData(@"""Settings"": { ""IncludeFormattedMessage"": true }", true, false, false)]
  [InlineData(@"""Exporters"": []", false, false, false)]
  public void Configure_SetsUpLoggingOptions_WhenAreConfigured(string jsonSeg, bool formatMsg, bool inclScope, bool parseState)
  {
      var jsonConfig = $@"{{ ""SimpleOpenTelemetry"": {{ ""Log"": {{ {jsonSeg} }} }} }}";

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;
        Assert.Equal(formatMsg, options.IncludeFormattedMessage);
        Assert.Equal(inclScope, options.IncludeScopes);
        Assert.Equal(parseState, options.ParseStateValues);
    }
}