using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public sealed class TimerService(TimeProvider timeProvider) : BackgroundService, ITimerService
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	private readonly ConcurrentBag<Func<Task>> _tick = [];
	private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1), timeProvider);

	public void Register(Func<Task> func)
	{
		Log.Info("Registered HTask: {0}", func.Method.Name);
		_tick.Add(func);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Log.Info("HTask service is starting.");

		await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, stoppingToken);

		while (await _timer.WaitForNextTickAsync(stoppingToken))
		{
			await ExecuteTickAsync();
		}
	}

	private async Task ExecuteTickAsync()
	{
		Log.Info("Running HTask");

		foreach (var func in _tick)
		{
			try
			{
				await func();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "HTask error in {0}", func.Method.Name);
			}
		}

		Log.Info("HTask done");
	}

	public override void Dispose()
	{
		_timer.Dispose();
		base.Dispose();
	}
}
