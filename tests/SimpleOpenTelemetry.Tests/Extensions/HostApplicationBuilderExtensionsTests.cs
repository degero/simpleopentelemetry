
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Extensions;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

public class HostApplicationBuilderExtensionsTests
{

    [Fact(Skip = "true")] // TODO Chad reinstate with eventlogging only option as this throws before app is built
    public void AddSimpleOpenTelemetry_ThrowsWhenSimpleOpenTelemetryConfigSignalSubSections_AreUndefined()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                ["SimpleOpenTelemetry"] = "{}"
            })
            .Build();

        var services = new ServiceCollection();

        // Act/assert
        Assert.ThrowsAny<Exception>(() => services.AddSimpleOpenTelemetry(config)); // Config section missing - no providers are created
    }
    
    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsWhen_AddSimpleOpenTelemetry_NotCalled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.SimpleOpenTelemetryValidate());
        Assert.Contains("OpenTelemetry has not been registered", exception.Message);
    }


}