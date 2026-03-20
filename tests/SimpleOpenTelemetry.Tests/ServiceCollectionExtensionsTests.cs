namespace SimpleOpenTelemetry.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Extensions;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    // TODO  implement tests
    [Fact]
    public void AddSimpleOpenTelemetry_RegistersTracerProvider()
    {
    }

    [Fact]
    public void AddSimpleOpenTelemetry_NullConfigureThrows()
    {
    }
}
