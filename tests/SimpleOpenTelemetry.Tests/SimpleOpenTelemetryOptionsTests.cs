using SimpleOpenTelemetry.Builder;
using Xunit;

namespace SimpleOpenTelemetryTests;

public class SimpleOpenTelemetryOptionsTests
{
    // TODO remove this AI slop
    [Fact]
    public void SimpleOpenTelemetryExporterConfig_AllowsSettingValues()
    {
        var config = new SimpleOpenTelemetryExporterConfig
        {
            Type = SimpleOpenTelemetryExporterType.Otlp
        };

        Assert.Equal(SimpleOpenTelemetryExporterType.Otlp, config.Type);
    }

}