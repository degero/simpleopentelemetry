using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleOpenTelemetry.Instrumentation
{
    public enum TracingInstrumentationEnum
    {
        AspNetCore,
        HttpClient,
        SqlClient,
        EFCore,
        GrpcNetClient,
        GrpcCore,
        Wcf,
        Hangfire,
        Quartz,
        StackExchangeRedis,
        AWS,
        AWSLambda,
    }
}
