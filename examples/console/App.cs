
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleOpenTelemetry.Examples.Console;

public class App : IHostedService
{
	private readonly ILogger<App> _logger;
	private readonly IHostApplicationLifetime _lifetime;

	private readonly ITestHttpCalls _httpCalls;

	public App(ILogger<App> logger, IHostApplicationLifetime lifetime, ITestHttpCalls testHttpCalls)
	{
		_logger = logger;
		_lifetime = lifetime;
		_httpCalls = testHttpCalls;

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
        // 1. DEMO calls to view in Grafana Loki and Tempo queries and Jaeger
        _logger.LogInformation("Test log message from Generic Host Console App");
        _logger.LogTrace("Test trace message Generic Host Console App");
        _logger.LogDebug("Test debug message Generic Host Console App");
        _logger.LogWarning("Test warning message Generic Host Console App");
        _logger.LogError("Test error message Generic Host Console App");
        _logger.LogCritical("Test critical message Generic Host Console App");

		await _httpCalls.DemonstrateHttpCalls();

	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
