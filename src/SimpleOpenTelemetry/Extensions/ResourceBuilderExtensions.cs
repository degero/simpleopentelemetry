using OpenTelemetry.Resources;
using SimpleOpenTelemetry.OtelComponents.Resource;

namespace SimpleOpenTelemetry.Extensions;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Enables SimpleOpenTelemetry assembly version resource detector.
    /// </summary>
    /// <param name="builder">The <see cref="ResourceBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="ResourceBuilder"/> being configured.</returns>
    public static ResourceBuilder AddAssemblyVersionDetector(this ResourceBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddDetector(new AssemblyVersionResourceDetector());
    }
}