using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using SimpleOpenTelemetry.Examples.AspNetCore.Data;
using SimpleOpenTelemetry.Extensions;


using var otelListener = new OtelEventListener();
using var simpleOtelListener = new SimpleOtelEventListener();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add EFCore if enabled in config
if(builder.Configuration.GetValue<string>("UseSqlEfCore").ToLower() == "true")
{
    builder.Services.AddDbContext<AppDbContext>(options => {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}
builder.Services.Configure<EntityFrameworkInstrumentationOptions>(options =>
{
    options.EnrichWithIDbCommand = (activity, command) =>
    {
        var stateDisplayName = $"{command.CommandType} main";
        activity.DisplayName = stateDisplayName;
        activity.SetTag("db.name", stateDisplayName);
    };
});


// OPTIONAL: clear loggers for
builder.Logging.ClearProviders();

// Register OpenTelemetry using SimpleOpenTelemetry
var otel = builder.AddSimpleOpenTelemetry();

// TODO put trace timer here

// Need to add in an event source by code if using the Azure monitor distro
var distro = builder.Configuration.GetValue<string>("SimpleOpenTelemetry:Distro");
if (string.Equals(distro, SimpleOpenTelemetry.Distro.DistroEnum.AzureMonitorAspNetCore.ToString(), StringComparison.OrdinalIgnoreCase))
    otel.WithTracing(r => r.AddSource("SimpleOpenTelemetry.Examples.AspNetCore.*"));

var app = builder.Build();

// Optional Validate OpenTelemetry using SimpleOpentelemetry tool
app.Services.SimpleOpenTelemetryValidate();

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



