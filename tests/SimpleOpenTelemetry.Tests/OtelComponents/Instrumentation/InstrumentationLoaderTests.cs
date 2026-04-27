using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Instrumentation;

public class InstrumentationLoaderTests
{
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

    [Theory]
    [MemberData(nameof(GetAllTraceInstrumentations), true)]
    [MemberData(nameof(GetAllTraceInstrumentations), false)]
    public void AddTracingInstrumentation_WithKnownEnum_LogsSuccessOrFailure(
        TraceInstrumentationEnum instrumentation,
        string assemblyName,
        bool packageInstalled)
    {
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }

        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var sut = new InstrumentationLoader(_configuration, packageInstalled ? _assemblyExec : mockAssemblyExec.Object);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithTracing(t =>
        {
            sut.AddTracingInstrumentation(t, instrumentation);
        });

        var successEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"registered trace instrumentation '{instrumentation}'") ?? false));
        var errorEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register trace instrumentation '{instrumentation}'") ?? false) &&
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

    [Theory]
    [MemberData(nameof(GetAllMetricInstrumentations), true)]
    [MemberData(nameof(GetAllMetricInstrumentations), false)]
    public void AddMetricsInstrumentation_WithKnownEnum_LogsSuccessOrFailure(
        MetricInstrumentationEnum instrumentation,
        string assemblyName,
        bool packageInstalled)
    {
        var mockAssemblyExec = new Mock<IAssemblyExecution>();
        if (!packageInstalled)
        {
            mockAssemblyExec
                .Setup(r => r.GetAssembly(assemblyName))
                .Throws(new Exception(
                    $"Cannot load assembly '{assemblyName}'. Ensure you have added the required nuget package to your project."));
        }

        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var sut = new InstrumentationLoader(_configuration, packageInstalled ? _assemblyExec : mockAssemblyExec.Object);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithMetrics(m =>
        {
            sut.AddMetricsInstrumentation(m, instrumentation);
        });

        var successEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"registered metric instrumentation '{instrumentation}'") ?? false));
        var errorEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
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
