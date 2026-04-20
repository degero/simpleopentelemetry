
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace SimpleOpenTelemetry.Examples.Console;

public class App : IHostedService
{
	private readonly ILogger<App> _logger;
	private readonly IHostApplicationLifetime _lifetime;
	private static readonly ActivitySource ActivitySource = new("SimpleOpenTelemetry.Examples.Console.App");

	public App(ILogger<App> logger, IHostApplicationLifetime lifetime)
	{
		_logger = logger;
		_lifetime = lifetime;

	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		// Register a callback so work starts *after* the host has fully started
		_lifetime.ApplicationStarted.Register(() =>
		{
			_ = RunAsync(cancellationToken);
		});
	}

	private async Task RunAsync(CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Starting work...");

			await DoWorkAsync(cancellationToken);

			_logger.LogInformation("Work complete.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception.");
		}
		finally
		{
			// Cleanly shut down the host when done
			_logger.LogInformation("Application stopping.");
			_lifetime.StopApplication();
		}
	}

	private async Task DoWorkAsync(CancellationToken cancellationToken)
	{
        _logger.LogInformation("Test log message from HomeController.Index");

        _logger.LogTrace("Test trace message from HomeController.Index");

		// Demonstrate various operations
		await DemonstrateHttpCalls();

	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	private async Task DemonstrateHttpCalls()
	{

		using (var activity = ActivitySource.StartActivity("DoSomeWork"))
		{
			_logger.LogInformation("\n📡 Demonstrating HTTP Client Instrumentation");

			activity!.SetTag("custom.tag", "hello");
			activity.SetStatus(ActivityStatusCode.Ok);
			var activityEvent = new ActivityEvent("Work");
			activity.AddEvent(activityEvent);

			var httpClient = new HttpClient();

			try
			{
				// Make a sample HTTP request
				List<string> urls = ["https://checkip.amazonaws.com", "https://api.github.com/users/torvalds"];

				foreach (var url in urls)
				{
					var response = await httpClient.GetAsync(url);

					if (response.IsSuccessStatusCode)
					{
						var content = await response.Content.ReadAsStringAsync();
						_logger.LogInformation($"   ✓ GET {url}");
						_logger.LogInformation($"   └─ Response: {content.Substring(0, Math.Min(50, content.Length))}...");
					}
					else
					{
						_logger.LogError("   ⚠ HTTP request failed: {StatusCode} {Content}", (int)response.StatusCode, await response.Content.ReadAsStringAsync());
					}
				}

			}
			catch (Exception ex)
			{
				_logger.LogError($"   ⚠ HTTP request failed: {ex.Message}");
			}
		}
	}
}
