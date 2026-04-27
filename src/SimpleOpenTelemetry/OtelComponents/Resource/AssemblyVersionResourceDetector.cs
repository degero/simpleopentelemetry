
using System.Reflection;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Utils;
using OtelResource = OpenTelemetry.Resources.Resource;

namespace SimpleOpenTelemetry.OtelComponents.Resource;

internal class AssemblyVersionResourceDetector : IResourceDetector
{
    /// <summary>
    /// Create an core resource attribute service.version if not defined from Env var / json configuration
    /// This examines the 'built' assembly version that may be set in a CICD pipleine and in msbuild
    /// Currently there is nothing in opentelemetry-dotnet-contrib to do this
    /// </summary>
    /// <returns></returns>
    public OtelResource Detect()
    {
       
        try 
        {
            // TODO Chad test on a dotnet build -p:Version=X
            var version = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?.Split('+')[0];

            if (!string.IsNullOrWhiteSpace(version))
                return new OtelResource(new List<KeyValuePair<string, object>>() { new (OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion, version) });
            else 
                return OtelResource.Empty;
        }
        catch
        {}

        return OtelResource.Empty;
    }
}