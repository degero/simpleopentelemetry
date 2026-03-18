using OpenTelemetry;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Setup otel
builder.Logging.ClearProviders();

// TODO chad move to simple open tel config ?
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

// Register OpenTelemetry using SimpleOpenTelemetry configuration-based setup
// TODO Chad look at way to not pass configuration like AddOpenTelemetry()
builder.Services.AddSimpleOpenTelemetry(builder.Configuration);

var app = builder.Build();

// Validate OpenTelemetry
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



