using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Instrumentation;

[CollectionDefinition("InstrumentationLoaderTests", DisableParallelization = true)]
public class InstrumentationLoaderTestsCollection {}

[Collection("InstrumentationLoaderTests")]
public class InstrumentationLoaderTests : IDisposable
{
    private readonly TestEventListener _listener;
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

    public InstrumentationLoaderTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
    [Theory]
    [MemberData(nameof(GetAllTraceInstrumentations), true)]
    [MemberData(nameof(GetAllTraceInstrumentations), false)]
    public void AddTracingInstrumentations_WithKnownEnum_LogsSuccessOrFailure(
        TraceInstrumentationEnum instrumentation,
        string assemblyName,
        bool packageInstalled)
    {
        // ARRANGE
        Assert.Empty(_listener.Events);

        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }

        
        var target = new InstrumentationLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var services = new ServiceCollection();

        // ACT
        services.AddOpenTelemetry().WithTracing(t =>
        {
            target.AddTracingInstrumentations(t, new SimpleOpenTelemetryOptions
            {
                Trace = new() { Instrumentations = [instrumentation]}
            });
        });

        // ASSERT
        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered trace instrumentation '{instrumentation}'") ?? false) ?? false));
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Failed to register trace instrumentation '{instrumentation}'") ?? false) ?? false) &&
            (e.Payload?.Any(p => p?.ToString()?.Contains("Ensure you have added the required nuget package to your project.") ?? false) ?? false));

        if (packageInstalled)
        {
            Assert.NotNull(successEvent);
            Assert.Null(errorEvent);
        }
        else
        {
            Assert.NotNull(errorEvent);
            Assert.Null(successEvent);
        }
    }

    [Theory]
    [MemberData(nameof(GetAllMetricInstrumentations), true)]
    [MemberData(nameof(GetAllMetricInstrumentations), false)]
    public void AddMetricsInstrumentations_WithKnownEnum_LogsSuccessOrFailure(
        MetricInstrumentationEnum instrumentation,
        string assemblyName,
        bool packageInstalled)
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }

        var target = new InstrumentationLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var services = new ServiceCollection();

        // ACT
        services.AddOpenTelemetry().WithMetrics(m =>
        {
            target.AddMetricsInstrumentations(m, new SimpleOpenTelemetryOptions
            {
                Metric = new() { Instrumentations = [instrumentation]}
            });
        });

        // ASSERT
        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered metric instrumentation '{instrumentation}'") ?? false));
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register metric instrumentation '{instrumentation}'") ?? false) &&
            e.Payload.Any(p => p?.ToString()?.Contains("Ensure you have added the required nuget package to your project.") ?? false));

        if (packageInstalled)
        {
            Assert.NotNull(successEvent);
            Assert.Null(errorEvent);
        }
        else
        {
            Assert.NotNull(errorEvent);
            Assert.Null(successEvent);
        }
    }

    [Fact]
    public void AddTracingInstrumentations_LogsErrorEvent_ForInvalidEnumValue()
    {
         // ARRANGE
        Assert.Empty(_listener.Events);
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Instrumentations:0"] = "999"
            })
            .Build();

        var services = new ServiceCollection();

        // ACT - more of an integration test to allow a non-enum string in
        services.AddSimpleOpenTelemetry(config);

        // ASSERT
        Assert.Contains(_listener.Events, r => r.EventId == 3 && 
            r.Level == EventLevel.Error &&
            r.Payload is not null &&
            r.Payload.Any(r => r?.ToString()?.Contains("type '999' not found ") ?? false));
    }

 [Fact]
    public void AddMetricInstrumentations_LogsErrorEvent_ForInvalidEnumValue()
    {
         // ARRANGE
        Assert.Empty(_listener.Events);
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Instrumentations:0"] = "999"
            })
            .Build();

        var services = new ServiceCollection();

        // ACT - more of an integration test to allow a non-enum string in
        services.AddSimpleOpenTelemetry(config);

        // ASSERT
        Assert.Contains(_listener.Events, r => r.EventId == 3 && 
            r.Level == EventLevel.Error &&
            r.Payload is not null &&
            r.Payload.Any(r => r?.ToString()?.Contains("type '999' not found ") ?? false));
    }

    public static IEnumerable<object[]> GetAllTraceInstrumentations(bool packageInstalled)
    {
        foreach (var instrumentation in InstrumentationAssemblies.KnownTraceInstrumentations)
        {
            yield return new object[] { instrumentation.Key, instrumentation.Value.AssemblyName, packageInstalled };
        }
    }

    public static IEnumerable<object[]> GetAllMetricInstrumentations(bool packageInstalled)
    {
        foreach (var instrumentation in InstrumentationAssemblies.KnownMetricsInstrumentations)
        {
            yield return new object[] { instrumentation.Key, instrumentation.Value.AssemblyName, packageInstalled };
        }
    }
}
