using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using soteltestazure.Models;

namespace soteltestazure.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly ActivitySource _activitySource;


    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;

        _activitySource = new ActivitySource("soteltestazure.Controllers.Home");

    }


    public IActionResult Index()
    {
        var controller = "HomeController"; 
        var action="Index";
        _logger.LogTrace("Test trace message from {Controller}.{Action}", controller, action);
        _logger.LogDebug("Test debug message from {Controller}.{Action}", controller, action);
        _logger.LogInformation("Test information message from {Controller}.{Action}", controller, action);
        _logger.LogWarning("Test warning message from {Controller}.{Action}", controller, action);
        _logger.LogError("Test error message from {Controller}.{Action}", controller, action);
        _logger.LogCritical("Test critical message from {Controller}.{Action}", controller, action);

        using (var activity = _activitySource.StartActivity("DoSomeWork"))
        {
            _logger.LogInformation("Test information message in Trace: DoSomeWork from {Controller}.{Action}", controller, action);

            if (activity != null && activity.IsAllDataRequested == true)
            {
                activity!.SetTag("custom.tag", "hello");
                activity.SetStatus(ActivityStatusCode.Ok);
                var activityEvent = new ActivityEvent("Work");
                activity.AddEvent(activityEvent);
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
