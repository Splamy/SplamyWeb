using CliWrap;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class QintController : ControllerBase
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly StoreService store;
		private readonly string build_qint_env = Path.Combine(Util.DataPath, "build_qint_env");
		private static CancellationTokenSource? currentBuild = null;
		public const string CiContext = "splamy-ci";
		public const string CiDescription = "Nightly build by Splamyserver";
		public const string CiStatusSuccess = "success";
		public const string CiStatusPending = "pending";
		public const string CiStatusError = "error";
		public const string CiStatusFailure = "failure";

		public QintController(StoreService store)
		{
			this.store = store;
		}

		[HttpPost("push")]
		public void Push([FromBody] GhPushEvent ev)
		{
			//var expect_secret = await store.Get("build_qint_webhook_secret");
			//if (ev?.hook?.config?.secret != expect_secret) return;

			// TODO renable after testing
			// if (ev?.@ref != "refs/heads/master") { Log.Info("Invalid branch"); return; }

			if (ev?.repository?.full_name != "ReSpeak/Qint") { Log.Info("Invalid project"); return; }
			var commit = ev?.after;
			if (commit is null) { Log.Info("No commit in webhook?"); return; }

			var current_cts = new CancellationTokenSource();
			var old_cts = Interlocked.Exchange(ref currentBuild, current_cts);
			if (old_cts != null)
			{
				old_cts.Cancel();
			}

			Run(commit, current_cts);
		}

		public async void Run(string commit, CancellationTokenSource cts)
		{
			using var _ = cts;

			try
			{
				await PushJson(new StateBody(CiStatusPending, CiContext, CiDescription), commit, cts.Token);

				System.IO.File.WriteAllLines(build_qint_env, new[] {
					$"QINT_SHA={commit}"
				});

				//await Cli.Wrap("systemctl")
				//	.WithArguments("start buildqint")
				//	.ExecuteAsync(cts.Token);
				await Cli.Wrap("bash")
					.WithArguments("/home/splamy/buildqint/build_qint.sh")
					.WithWorkingDirectory("/home/splamy/buildqint")
					.ExecuteAsync(cts.Token);

				// TODO read output ?

				await PushJson(new StateBody(CiStatusSuccess, CiContext, CiDescription), commit);
			}
			catch (OperationCanceledException)
			{
				// TODO stop systemctl

				await PushJson(new StateBody(CiStatusFailure, CiContext, "Cancelled"), commit);
			}
			catch (Exception ex)
			{
				Log.Error("Build {0} failed: {1}", commit, ex.Message);
				await PushJson(new StateBody(CiStatusFailure, CiContext, "Failed: " + ex.Message), commit);
			}

			Interlocked.CompareExchange(ref currentBuild, null, cts);
		}

		[HttpPost("test_set_status")]
		public async Task TestStatus([FromQuery] string sha, [FromQuery] string status)
		{
			_ = await PushJson(new StateBody(status, CiContext, CiDescription), sha);
		}

		private async Task<bool> PushJson(StateBody data, string sha, CancellationToken ct = default)
		{
			try
			{
				var content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), Util.JsonDefault);
				var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/ReSpeak/Qint/statuses/{sha}")
				{
					Content = content,
				};
				request.Headers.Authorization = new AuthenticationHeaderValue("Basic", await store.GetGithubAuth());
				using var response = await Util.httpClient.SendAsync(request, ct);
				response.EnsureSuccessStatusCode();
				return true;
			}
			catch (HttpRequestException ex)
			{
				Log.Warn(ex, "Error posting to github: " + ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error accessing github");
				return false;
			}
		}
	}

	public class GhPushEvent
	{
		// The pushed ref
		public string? @ref { get; set; }
		public string? before { get; set; }
		public string? after { get; set; }
		public GhPushEventRepository? repository { get; set; }
	}

	public class GhPushEventRepository
	{
		public string? full_name { get; set; }
	}

	public record StateBody(string state, string context, string description);
}
