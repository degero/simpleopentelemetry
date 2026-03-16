using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleOpenTelemetry.Instrumentation
{
  
    public enum MetricsInstrumentationEnum
    {
        AspNetCore,
        HttpClient,
        SqlClient,
        Runtime,
        Process,
        Hangfire,
    }
}
