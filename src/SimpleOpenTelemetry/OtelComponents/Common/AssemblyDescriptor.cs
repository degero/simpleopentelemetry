namespace SimpleOpenTelemetry.OtelComponents.Common;

internal record AssemblyDescriptor(
     string AssemblyName,
     string TypeName,
     string[]? MethodNames = null,
     string? OptionsClassName = null,
     bool OptionsRequired = false
);