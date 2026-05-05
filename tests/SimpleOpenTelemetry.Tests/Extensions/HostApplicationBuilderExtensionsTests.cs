
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Extensions;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

public class HostApplicationBuilderExtensionsTests
{
    [Fact]
    public void AddSimpleOpenTelemetry_Should_Throw_WhenBuilderIsNull()
    {
        // Arrange
        IHostApplicationBuilder? builder = null;

        // Act/assert
        Assert.Throws<ArgumentNullException>(() =>
            HostApplicationBuilderExtensions.AddSimpleOpenTelemetry(builder!));
    }

    [Fact]
    public void AddSimpleOpenTelemetry_Should_Call_ServiceExtensionAddSimpleOpenTelemetry()
    {
        // Arrange
        var mockIConfiguration = new Mock<IConfigurationManager>();

        var configRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Settings:SetErrorStatusOnException"] = "true",
            })
            .Build();
            
        mockIConfiguration.Setup(r => r.GetSection(SimpleOpenTelemetryOptions.SectionName)).Returns(configRoot.GetSection(SimpleOpenTelemetryOptions.SectionName));
        var mockIserviceCollection = new Mock<IServiceCollection>();
        var services = new ServiceCollection();
        var builder = new Mock<IHostApplicationBuilder>();
        builder.SetupGet(r => r.Configuration).Returns(mockIConfiguration.Object);
        builder.SetupGet(r => r.Services).Returns(services);

        // Act
        HostApplicationBuilderExtensions.AddSimpleOpenTelemetry(builder.Object);

        // Assert -  not idea but as it is an extension we need to verify an outcome of the underlying service to add tracerprovider
        // collection extension AddSimpleOpenTelemetry() 
        Assert.Contains(services, sd => sd.ServiceType.ToString().Contains("TracerProvider"));
    }
}