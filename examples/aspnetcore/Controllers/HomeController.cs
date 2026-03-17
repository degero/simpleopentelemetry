using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimpleOpenTelemetry.Examples.AspNetCoreModels;

namespace SimpleOpenTelemetry.Examples.AspNetCoreControllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly ActivitySource _activitySource;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
        // activity source name has to match the registere OTEL_SERVICE_NAME setting
        _activitySource = new ActivitySource("demo-aspnet-simpleopentelemetry");
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
            var activityEvent = new ActivityEvent("ProductsRetrieved",
               tags: new ActivityTagsCollection { new("products.count", 1) });
            activity.AddEvent(activityEvent);
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
