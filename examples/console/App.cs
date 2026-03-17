
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SimpleOpenTelemetry.Examples.Console;

public class App : IHostedService
{
	private readonly ILogger<App> _logger;
	private readonly IHostApplicationLifetime _lifetime;
	private static readonly ActivitySource ActivitySource = new("DemoConsoleApp");

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
		// Demonstrate various operations
		using var activity = ActivitySource.StartActivity("Demonstrating calls");
		await DemonstrateHttpCalls();
		await DemonstrateComplexOperations(_logger);

	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	private async Task DemonstrateHttpCalls()
	{
		_logger.LogInformation("\n📡 Demonstrating HTTP Client Instrumentation");
		_logger.LogInformation("   (These calls are automatically traced)\n");

		var httpClient = new HttpClient();

		try
		{
			// Make a sample HTTP request
			var sw = Stopwatch.StartNew();
			var response = await httpClient.GetAsync("https://api.github.com/zen");
			sw.Stop();

			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
				_logger.LogInformation($"   ✓ GET https://api.github.com/zen ({sw.ElapsedMilliseconds}ms)");
				_logger.LogInformation($"   └─ Response: {content.Substring(0, Math.Min(50, content.Length))}...");
			}
		}
		catch (Exception ex)
		{
			_logger.LogInformation($"   ⚠ HTTP request failed: {ex.Message}");
		}
	}

	private async Task DemonstrateComplexOperations(ILogger logger)
	{
		_logger.LogInformation("\n🔧 Demonstrating Complex Operations");
		_logger.LogInformation("   (With error handling and retries)\n");
		logger.LogInformation("Demonstrating Complex Operations");

		try
		{
			// Simulate business logic with timing
			_logger.LogInformation("   Step 1: Initializing data processing...");
			logger.LogInformation("Step 1: Initializing data processing...");

			await Task.Delay(100);

			_logger.LogInformation("   Step 2: Processing batch of 10 items...");
			logger.LogInformation("Step 2: Processing batch of 10 items...");

			var sw = Stopwatch.StartNew();
			for (int i = 0; i < 10; i++)
			{
				// Simulate processing
				await Task.Delay(1);
			}
			sw.Stop();
			_logger.LogInformation($"   ✓ Batch processing completed in {sw.ElapsedMilliseconds}ms");
			logger.LogInformation("Step 2: Processing batch of 100 items...");

			_logger.LogInformation("   Step 3: Validating results...");
			logger.LogInformation("Step 3: Validating results...");

			await Task.Delay(50);
			_logger.LogInformation("   ✓ All validations passed");
			logger.LogInformation("All validations passed");

			_logger.LogInformation("   Step 4: Finalizing...");
			logger.LogInformation("Step 4: Finalizing...");

			await Task.Delay(25);

			_logger.LogInformation("   ✓ Operation complete");
			logger.LogInformation("Operation complete");
		}
		catch (Exception ex)
		{
			_logger.LogInformation($"   ✗ Operation failed: {ex.Message}");
			logger.LogError(ex, "Operation failed");
		}
	}
}
