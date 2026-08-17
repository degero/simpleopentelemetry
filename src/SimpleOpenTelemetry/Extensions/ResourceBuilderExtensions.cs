using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Internal;
using SimpleOpenTelemetry.OtelComponents.Resource;

namespace SimpleOpenTelemetry.Extensions;

/// <summary>
/// Extension methods for SimpleOpenTelemetry resource detectors.
/// </summary>
public static class ResourceBuilderExtensions
{
    /// <summary>
    /// Enables SimpleOpenTelemetry assembly version resource detector.
    /// </summary>
    /// <param name="builder">The <see cref="ResourceBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="ResourceBuilder"/> being configured.</returns>
    public static ResourceBuilder AddAssemblyVersionDetector(this ResourceBuilder builder)
    {
        Guard.ThrowIfNull(builder);
        return builder.AddDetector(new AssemblyVersionResourceDetector());
    }
}
