using SimpleOpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register OpenTelemetry using SimpleOpenTelemetry
builder.AddSimpleOpenTelemetry();

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



