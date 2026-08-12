using System.Reflection;
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
        // ARRANGE
        var builder = ResourceBuilder.CreateEmpty();
        builder.AddAssemblyVersionDetector();
        // the test framework is the entry assembly the resource detector will find
        var testFrameworkVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?.Split('+')[0];

        // ACT
        var resource = builder.Build();

        // ASSERT
        Assert.Contains(resource.Attributes,
            a => a.Key == "service.version" && a.Value.ToString() == testFrameworkVersion);

    }
}
