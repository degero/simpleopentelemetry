using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.OtelComponents.Extension;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Extension;

public class ExtensionLoaderTests
{
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

    [Theory]
    [MemberData(nameof(GetAllTraceExtensions), true)]
    [MemberData(nameof(GetAllTraceExtensions), false)]
    public void AddTraceExtension_WithKnownTraceExtension_LogsSuccessOrFailure(
        TraceExtensionsEnum extension,
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
        var sut = new ExtensionLoader(_configuration, packageInstalled ? _assemblyExec : mockAssemblyExec.Object);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithTracing(t =>
        {
            sut.AddTraceExtension(t, extension);
        });

        var successEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"registered trace extension '{extension}'") ?? false));
        var errorEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register trace extension '{extension}'") ?? false) &&
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
    public void AddMetricsExtension_WithNoneEnum_LogsMissingDescriptorError()
    {
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var sut = new ExtensionLoader(_configuration, _assemblyExec);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithMetrics(m =>
        {
            sut.AddMetricsExtension(m, MetricExtensionsEnum.None);
        });

        var errorEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains("MetricExtensionsEnum type 'None' not found to initialise metric extension.") ?? false));

        Assert.NotNull(errorEvent);
    }

    [Fact]
    public void AddLogExtension_WithNoneEnum_LogsMissingDescriptorError()
    {
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var sut = new ExtensionLoader(_configuration, _assemblyExec);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithLogging(l =>
        {
            sut.AddLogExtension(l, LogExtensionsEnum.None);
        });

        var errorEvent = listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains("LogExtensionsEnum type 'None' not found to initialise log extension.") ?? false));

        Assert.NotNull(errorEvent);
    }

    public static IEnumerable<object[]> GetAllTraceExtensions(bool packageInstalled)
    {
        foreach (var extension in ExtensionAssemblies.KnownTraceExtensions)
        {
            yield return new object[] { extension.Key, extension.Value.AssemblyName, packageInstalled };
        }
    }
}
