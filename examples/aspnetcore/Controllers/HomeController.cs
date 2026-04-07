using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimpleOpenTelemetry.Examples.AspNetCore.Data;
using SimpleOpenTelemetry.Examples.AspNetCore.Models;

namespace SimpleOpenTelemetry.Examples.AspNetCore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly ActivitySource _activitySource;

    private readonly AppDbContext _context;


    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, AppDbContext context)
    {
        _logger = logger;

        // activity source name has to match the registere OTEL_SERVICE_NAME setting 
        // this could be set to anything so long as the config setting TraceSources matches
        _activitySource = new ActivitySource(Utils.SettingsHelper.OtelServiceName(configuration));

        _context = context;
    }


    public IActionResult Index()
    {
        // ## DEMO calls to view in Grafana Loki and Tempo queries

        // 2. LOG → goes to Tempo
        // Write test to log
        _logger.LogInformation("Test log message from HomeController.Index");

        // Write test to traces
        _logger.LogTrace("Test trace message from HomeController.Index");

        // 2. SPAN → goes to Tempo
        using (var activity = _activitySource.StartActivity("DoSomeWork"))
        {
            activity!.SetTag("custom.tag", "hello");
            activity.SetStatus(ActivityStatusCode.Ok);
            var activityEvent = new ActivityEvent("Work");
            activity.AddEvent(activityEvent);
        }

        // 3. Use EF traces with the EFCore + SqlClient instrumentations
        using (var efActivity = _activitySource.StartActivity("EFCoreGetProducts"))
        {
            efActivity.SetStatus(ActivityStatusCode.Ok);
            var products = _context.Products.ToList();
            var activityEvent = new ActivityEvent("ProductsRetrieved",
               tags: new ActivityTagsCollection { new("products.count", products.Count()) });
            efActivity.AddEvent(activityEvent);
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
