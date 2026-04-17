using SimpleOpenTelemetry.OtelComponents.Exporter;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Exporter;

public class ExporterAssembliesTests
{
    [Fact]
    public void ExporterExtensionDescriptor_StoresConstructorValues()
    {
        var descriptor = new ExporterExtensionDescriptor(
            "Assembly.Name",
            "Type.Name",
            "MethodName",
            "AssemblyExtensionActionOptionsClassName");

        Assert.Equal("Assembly.Name", descriptor.AssemblyName);
        Assert.Equal("Type.Name", descriptor.TypeName);
        Assert.Equal("MethodName", descriptor.MethodName);
        Assert.Equal("AssemblyExtensionActionOptionsClassName", descriptor.OptionsClassName);
    }

    [Fact]
    public void KnownTraceExporters_ContainsAzureDescriptor()
    {
        Assert.True(ExporterAssemblies.KnownTraceExporters.ContainsKey(TraceExporterEnum.Azure));

        var descriptor = ExporterAssemblies.KnownTraceExporters[TraceExporterEnum.Azure];
        Assert.Equal("Azure.Monitor.OpenTelemetry.Exporter", descriptor.AssemblyName);
        Assert.Equal("AddAzureMonitorTraceExporter", descriptor.MethodName);
    }

    [Fact]
    public void KnownMetricsExporters_ContainsAzureDescriptor()
    {
        Assert.True(ExporterAssemblies.KnownMetricsExporters.ContainsKey(MetricExporterEnum.Azure));

        var descriptor = ExporterAssemblies.KnownMetricsExporters[MetricExporterEnum.Azure];
        Assert.Equal("Azure.Monitor.OpenTelemetry.Exporter", descriptor.AssemblyName);
        Assert.Equal("AddAzureMonitorMetricExporter", descriptor.MethodName);
    }

    [Fact]
    public void KnownLogExporters_ContainsAzureDescriptor()
    {
        Assert.True(ExporterAssemblies.KnownLogExporters.ContainsKey(LogExporterEnum.Azure));

        var descriptor = ExporterAssemblies.KnownLogExporters[LogExporterEnum.Azure];
        Assert.Equal("Azure.Monitor.OpenTelemetry.Exporter", descriptor.AssemblyName);
        Assert.Equal("AddAzureMonitorLogExporter", descriptor.MethodName);
    }
}

