using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SimpleOpenTelemetry.Examples.Console;

public interface ITestHttpCalls
{
    Task DemonstrateHttpCalls();
}

public class TestHttpCalls : ITestHttpCalls
{
    private static readonly ActivitySource ActivitySource = new("SimpleOpenTelemetry.Examples.Console.App");

    private readonly ILogger<TestHttpCalls> _logger;

    public TestHttpCalls(ILogger<TestHttpCalls> logger)
    {
        _logger = logger;
    }

    public async Task DemonstrateHttpCalls()
    {
        
        using (var activity = ActivitySource.StartActivity("DoSomeWork"))
        {
            _logger.LogInformation("\n📡 Demonstrating HTTP Client Instrumentation");

            if (activity != null && activity.IsAllDataRequested == true)
            {
                activity!.SetTag("custom.tag", "hello");
                activity.SetStatus(ActivityStatusCode.Ok);
                var activityEvent = new ActivityEvent("Work");
                activity.AddEvent(activityEvent);
            }
        }

        using (var activity = ActivitySource.StartActivity("MakeHttpCalls"))
        {
            var httpClient = new HttpClient();

            try
            {
                List<string> urls = ["https://checkip.amazonaws.com", "https://api.github.com/users/torvalds"];

                foreach (var url in urls)
                {
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        activity!.SetTag("response", "success");
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"   ✓ GET {url}");
                        _logger.LogInformation($"   └─ Response: {content.Substring(0, Math.Min(50, content.Length))}...");
                    }
                    else
                    {
                        activity!.SetTag("response", "failure");
                        _logger.LogError("   ⚠ HTTP request failed: {StatusCode} {Content}", (int)response.StatusCode, await response.Content.ReadAsStringAsync());
                    }
                }

            }
            catch (Exception ex)
            {
                activity!.SetTag("response", "failure");
                _logger.LogError($"   ⚠ HTTP request failed: {ex.Message}");
            }
        }
    }
}