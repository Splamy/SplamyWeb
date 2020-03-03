using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class SlowTimer : IHostedService, IDisposable
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private Timer? timer;

		private readonly List<Func<Task>> tick = new List<Func<Task>>();

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Log.Info("HTask service is starting.");

			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // TODO !! HOURS 

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
				catch (Exception ex) { Log.Error(ex, "HTask error"); }
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
}
