using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimpleOpenTelemetry.Examples.AspNetCore.Data;
using SimpleOpenTelemetry.Examples.AspNetCore.Models;

namespace SimpleOpenTelemetry.Examples.AspNetCore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly ActivitySource _activitySource;

    private readonly AppDbContext? _context;

    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, AppDbContext? context = null)
    {
        _logger = logger;

        _activitySource = new ActivitySource("SimpleOpenTelemetry.Examples.AspNetCore.Controllers.Home");

        _context = context ?? null;

        _configuration = configuration;
    }


    public IActionResult Index()
    {
        // 1. DEMO calls to view in Grafana Loki and Tempo queries and Jaeger
        _logger.LogInformation("Test log message from HomeController.Index");
        _logger.LogTrace("Test trace message from HomeController.Index");
        _logger.LogDebug("Test debug message from HomeController.Index");
        _logger.LogWarning("Test warning message from HomeController.Index");
        _logger.LogError("Test error message from HomeController.Index");
        _logger.LogCritical("Test critical message from HomeController.Index");

        // 2. SPAN → goes to Tempo
        using (var activity = _activitySource.StartActivity("DoSomeWork"))
        {
            activity!.SetTag("custom.tag", "hello");
            activity.SetStatus(ActivityStatusCode.Ok);
            var activityEvent = new ActivityEvent("Work");
            activity.AddEvent(activityEvent);
        }

        // 3. If enabled Use EF traces with the EFCore + SqlClient instrumentations
        if(_configuration.GetValue<string>("UseSqlEfCore").ToLower() == "true")
        {
            using (var efActivity = _activitySource.StartActivity("GetProducts"))
            {
                efActivity.SetStatus(ActivityStatusCode.Ok);
                var products = _context.Products.ToList();
                var activityEvent = new ActivityEvent("ProductsRetrieved",
                tags: new ActivityTagsCollection { new("products.count", products.Count()) });
                efActivity.AddEvent(activityEvent);
            }
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
