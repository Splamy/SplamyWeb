using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public sealed class TimerService : BackgroundService, IDisposable
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	private readonly ConcurrentBag<Func<Task>> tick = new();
	private readonly PeriodicTimer timer = new(TimeSpan.FromHours(1));

	public void Register(Func<Task> func)
	{
		Log.Info("Registered HTask: {0}", func.Method.Name);
		tick.Add(func);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Log.Info("HTask service is starting.");

		await Task.Delay(1000, stoppingToken);

		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			await ExecuteTickAsync();
		}
	}

	private async Task ExecuteTickAsync()
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

	public override void Dispose()
	{
		timer.Dispose();
		base.Dispose();
	}

}
