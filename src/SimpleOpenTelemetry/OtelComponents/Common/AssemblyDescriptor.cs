namespace SimpleOpenTelemetry.OtelComponents.Common;

internal record AssemblyDescriptor(
     string AssemblyName,
     string TypeName,
     string? MethodName,
     string? OptionsClassName = null,
     bool OptionsRequired = false
);