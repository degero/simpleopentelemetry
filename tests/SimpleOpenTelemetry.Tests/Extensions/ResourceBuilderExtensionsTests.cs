using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Extensions;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

public class ResourceBuilderExtensionsTests
{

    [Fact]
    public void AddAssemblyVersionDetector_Should_Throw_WhenBuilderIsNull()
    {
        ResourceBuilder? builder = null;

        Assert.Throws<ArgumentNullException>(() =>
            SimpleOpenTelemetry.Extensions.ResourceBuilderExtensions.AddAssemblyVersionDetector(builder!));
    }

    [Fact]
    public void AddAssemblyVersionDetector_Should_AddDetector()
    {
        var builder = ResourceBuilder.CreateEmpty();
        
        builder.AddAssemblyVersionDetector();

        var resource = builder.Build();
        Assert.Contains(resource.Attributes, 
            a => a.Key == "service.version" && a.Value.ToString() == "18.4.0");

    }
}
