using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using soteltestaws.Models;

namespace soteltestaws.Controllers;

public class HomeController : Controller
{
   private readonly ILogger<HomeController> _logger;

    private readonly ActivitySource _activitySource;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;

         _activitySource = new ActivitySource("soteltestaws.Controllers.Home");
    }


    public IActionResult Index()
    {
        // 1. DEMO calls to view in Grafana Loki and Tempo queries and Jaeger
        _logger.LogTrace("Test trace message from HomeController.Index");
        _logger.LogDebug("Test debug message from HomeController.Index");
        _logger.LogInformation("Test information message from HomeController.Index");
        _logger.LogWarning("Test warning message from HomeController.Index");
        _logger.LogError("Test error message from HomeController.Index");
        _logger.LogCritical("Test critical message from HomeController.Index");

        // 2. SPAN → goes to Tempo
        using (var activity = _activitySource.StartActivity("DoSomeWork"))
        {
            if (activity != null && activity.IsAllDataRequested == true)
            {
                activity!.SetTag("custom.tag", "hello");
                activity.SetStatus(ActivityStatusCode.Ok);
                var activityEvent = new ActivityEvent("Work");
                activity.AddEvent(activityEvent);
                activity.Dispose();
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
