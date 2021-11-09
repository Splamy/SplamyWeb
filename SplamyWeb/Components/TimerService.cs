using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public sealed class TimerService : IHostedService, IDisposable
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private Timer? timer;

	private readonly ConcurrentBag<Func<Task>> tick = new();

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Log.Info("HTask service is starting.");

		async void StartTimerDelayed()
		{
			// Wait a second for other services to register first
			await Task.Delay(1000, cancellationToken);

			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
		}
		StartTimerDelayed();

		return Task.CompletedTask;
	}

	public void Register(Func<Task> func)
	{
		Log.Info("Registered HTask: {0}", func.Method.Name);
		tick.Add(func);
	}

	private async void DoWork(object? state)
	{
		Log.Info("Running HTask");

		foreach (var func in tick)
		{
			try
			{
				await func();
			}
			catch (Exception ex) { Log.Error(ex, "HTask error in {0}", func.Method.Name); }
		}

		Log.Info("HTask done");
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Log.Info("HTask service is stopping.");

		timer?.Change(Timeout.Infinite, 0);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		timer?.Dispose();
	}
}
