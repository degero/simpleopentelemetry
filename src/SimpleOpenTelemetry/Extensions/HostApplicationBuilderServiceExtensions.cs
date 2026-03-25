using Microsoft.Extensions.Hosting;

namespace SimpleOpenTelemetry.Extensions;

public static class HostApplicationBuilderExtensions
{

    public static IHostApplicationBuilder AddSimpleOpenTelemetry(
        this IHostApplicationBuilder builder)
    {
        builder.Logging.AddSimpleOpenTelemetry();

        // TODO Chad look at way to not pass configuration like AddOpenTelemetry()
        builder.Services.AddSimpleOpenTelemetry(builder.Configuration);

        return builder;
    }

}