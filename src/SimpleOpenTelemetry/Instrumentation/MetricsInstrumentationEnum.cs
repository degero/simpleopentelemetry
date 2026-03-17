using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SimpleOpenTelemetry.Instrumentation;

// TODO Chad check these as some may be missing
public enum MetricsInstrumentationEnum
{
    AspNetCore,
    HttpClient,
    SqlClient,
    Runtime,
    Process,
    Hangfire,
    AWS
}
