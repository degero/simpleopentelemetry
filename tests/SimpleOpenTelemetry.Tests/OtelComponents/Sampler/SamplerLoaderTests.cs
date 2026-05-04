using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Sampler;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Sampler;

public class SamplerLoaderTests: IDisposable
{
    private readonly TestEventListener _listener;
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();

    public SamplerLoaderTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact] // Placeholder until samplers added
    public void AddSampler_WithNoneEnum_LogsMissingDescriptorError()
    {
        // ARRANGE
        var target = new SamplerLoader(_assemblyExec);
        var builder = Host.CreateApplicationBuilder();
        var noneSampler = SamplerEnum.None.ToString();
        // ACT
        builder.Services.AddOpenTelemetry().WithTracing(t =>
        {
            target.AddSampler(t, new SimpleOpenTelemetryOptions{ Trace = new(){ Sampler = noneSampler}});
        });

        // ASSERT
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"type not found: {noneSampler} to initialize sampler") ?? false));

        Assert.NotNull(errorEvent);
    }

    [Theory(Skip="true")] // skipped until AWS xrayid sampler follows normal patterns of build time resource dependency resolution
    [MemberData(nameof(GetKnownSamplers), true)]
    [MemberData(nameof(GetKnownSamplers), false)]
    public void AddSampler_WithKnownSampler_LogsSuccessOrFailure(
        SamplerEnum sampler,
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

        
        var target = new SamplerLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var options = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Sampler = sampler.ToString()
            }
        };
        var resource = ResourceBuilder.CreateDefault().Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithTracing(t =>
        {
            target.AddSampler(t, options);
        });

        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered sampler '{sampler}'.") ?? false));
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register sampler '{sampler}'.") ?? false) &&
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
    public void AddSampler_WithUnsupportedSampler_LogsUnsupportedSamplerError()
    {
        
        var target = new SamplerLoader(_assemblyExec);
        var options = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Sampler = "NoSuchSampler"
            }
        };
        var resource = ResourceBuilder.CreateDefault().Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddOpenTelemetry().WithTracing(t =>
        {
            target.AddSampler(t, options);
        });

        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains("Unsupported OpenTelemetry sampler 'NoSuchSampler'.") ?? false));

        Assert.NotNull(errorEvent);
    }

    public static IEnumerable<object[]> GetKnownSamplers(bool packageInstalled)
    {
        foreach (var entry in SamplerAssemblies.KnownSamplers)
        {
            yield return new object[] { entry.Key, entry.Value.AssemblyName, packageInstalled };
        }
    }
}
