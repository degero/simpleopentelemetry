using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.SqlClient;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Examples.AspNetCore.Data;
using SimpleOpenTelemetry.Examples.Shared;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.OtelComponents.Resource;

// Add Event listeners outputting to console for demo/debug purposes
using var otelListener = new OtelEventListener();
using var simpleOtelListener = new SimpleOtelEventListener();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add EFCore if enabled in config
if (builder.Configuration.GetValue<string>("UseSqlEfCore")?.ToLower() == "true")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}

// Set extra options to make efcore instrumentation more helpful
// See readme for moe options
// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/19db4b8e6bb8821f89450693b14e609793452351/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/README.md
builder.Services.Configure<EntityFrameworkInstrumentationOptions>(options =>
{
    // Adds in helpful name to EF tracing
    options.EnrichWithIDbCommand = (activity, command) =>
    {
        var stateDisplayName = $"EFCoreCmd{command.CommandType}";
        activity.DisplayName = stateDisplayName;
        // other custom activity.SetTag() etc can be set here
    };
});

// Set extra options to make sqlclient instrumentation more helpful
// See readme for moe options
// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/19db4b8e6bb8821f89450693b14e609793452351/src/OpenTelemetry.Instrumentation.SqlClient/README.md
builder.Services.Configure<SqlClientTraceInstrumentationOptions>(options =>
{
    options.RecordException = true; // record exception on failure
    options.EnrichWithSqlCommand = (activity, obj) =>
    {
        if (obj is SqlCommand cmd)
        {
            activity.DisplayName = $"SqlClientCmd{cmd.CommandType}";
            activity.SetTag("db.commandTimeout", cmd.CommandTimeout);
        }
    };
});

// OPTIONAL: clear loggers so only the OpenTelemetry logger is attached
//builder.Logging.ClearProviders();

var sw = Stopwatch.StartNew();

// The entry point for SimpleOpenTelemetry lib to setup your OpenTelemetry
var otelBuilder = builder.AddSimpleOpenTelemetry();

sw.Stop();
Console.WriteLine($"AddSimpleOpenTelemetry() took: {sw.ElapsedMilliseconds}ms");

var app = builder.Build();

// OPTIONAL: Validate OpenTelemetry using SimpleOpentelemetry extension method
var valid = app.Services.SimpleOpenTelemetryValidate();
Console.WriteLine($"SimpleOpenTelemetryValidate result: {valid}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();



