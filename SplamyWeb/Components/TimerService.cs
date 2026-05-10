using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SplamyWeb.Components;

public sealed class TimerService(TimeProvider timeProvider, ILogger<TimerService> logger)
	: BackgroundService, ITimerService
{
	private readonly ConcurrentBag<Func<Task>> _tick = [];
	private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1), timeProvider);

	public void Register(Func<Task> func)
	{
		logger.LogInformation("Registered HTask: {Name}", func.Method.Name);
		_tick.Add(func);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("HTask service is starting.");

		await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, stoppingToken);

		while (await _timer.WaitForNextTickAsync(stoppingToken))
		{
			await ExecuteTickAsync();
		}
	}

	private async Task ExecuteTickAsync()
	{
		logger.LogInformation("Running HTask");

		foreach (var func in _tick)
		{
			try
			{
				await func();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "HTask error in {Name}", func.Method.Name);
			}
		}

		logger.LogInformation("HTask done");
	}

	public override void Dispose()
	{
		_timer.Dispose();
		base.Dispose();
	}
}
