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

[CollectionDefinition("SamplerLoaderTests", DisableParallelization = true)]
public class SamplerLoaderTestsCollection {}

[Collection("SamplerLoaderTests")]
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
        Assert.Empty(_listener.Events);

        var target = new SamplerLoader(_assemblyExec);
        var services = new ServiceCollection();
        var noneSampler = SamplerEnum.None.ToString();
        
        // ACT
        services.AddOpenTelemetry().WithTracing(t =>
        {
            target.SetSampler(t, new SimpleOpenTelemetryOptions{ Trace = new(){ Sampler = noneSampler}});
        });

        // ASSERT
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"Failed to register OpenTelemetry Sampler '{noneSampler}'.") ?? false) ?? false));

        Assert.NotNull(errorEvent);
    }

    [Theory(Skip="true")] // skipped until AWS xrayid OpenTelemetry Sampler follows normal patterns of build time resource dependency resolution
    [MemberData(nameof(GetKnownSamplers), true)]
    [MemberData(nameof(GetKnownSamplers), false)]
    public void AddSampler_WithKnownSampler_LogsSuccessOrFailure(
        SamplerEnum sampler,
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

        
        var target = new SamplerLoader(packageInstalled ? _assemblyExec : mockAssemblyExec.Object);
        var options = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Sampler = sampler.ToString()
            }
        };
        var resource = ResourceBuilder.CreateDefault().Build();
        var services = new ServiceCollection();

        // ACT
        services.AddOpenTelemetry().WithTracing(t =>
        {
            target.SetSampler(t, options);
        });

        // ASSERT
        var successEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Verbose &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Sampler '{sampler}'.") ?? false));
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Failed to register OpenTelemetry Sampler '{sampler}'.") ?? false) &&
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
         // ARRANGE
        Assert.Empty(_listener.Events);

        var target = new SamplerLoader(_assemblyExec);
        var options = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Sampler = "NoSuchSampler"
            }
        };
        var resource = ResourceBuilder.CreateDefault().Build();
        var services = new ServiceCollection();

        // ACT
        services.AddOpenTelemetry().WithTracing(t =>
        {
            target.SetSampler(t, options);
        });

        // ASSERT
        var errorEvent = _listener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"OpenTelemetry Sampler {typeof(SamplerEnum).Name} type 'NoSuchSampler' not found to initialise. Please check your SimpleOpenTelemetry configuration.") ?? false));

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
