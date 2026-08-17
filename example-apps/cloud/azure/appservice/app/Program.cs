using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

OtelEventListener? otelListener = null;
SimpleOtelEventListener? simpleOtelListener = null;

// FOR DEMO/DEBUG PURPOSES - Add Event listeners outputing to console
if (builder.Configuration.GetValue("EnableOtelEventListeners", false))
{
    otelListener = new OtelEventListener();
    simpleOtelListener = new SimpleOtelEventListener();
}

var otelBuilder = builder.AddSimpleOpenTelemetry();

// OPTIONAL: clear loggers so only the OpenTelemetry logger is attached
//builder.Logging.ClearProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    // cleanup eventlisteners
    if (otelListener is not null)
        otelListener.Dispose();
    if (simpleOtelListener is not null)
        simpleOtelListener.Dispose();
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
