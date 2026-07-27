


// // Need to add in an event source by code if using the Azure monitor distro
// var distro = builder.Configuration.GetValue<string>("SimpleOpenTelemetry:Distro");
// if (string.Equals(distro, SimpleOpenTelemetry.OtelComponents.Distro.DistroEnum.AzureMonitorAspNetCore.ToString(), StringComparison.OrdinalIgnoreCase))
//     otelBuilder.WithTracing(r => r.AddSource("SimpleOpenTelemetry.Examples.AspNetCore.*"));
