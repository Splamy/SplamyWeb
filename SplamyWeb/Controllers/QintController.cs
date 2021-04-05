using CliWrap;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
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
		private static BuildTask? currentBuild = null;
		public const string CiContext = "splamy-ci";
		public const string CiDescription = "Nightly build by Splamyserver";
		public const string CiStatusSuccess = "success";
		public const string CiStatusPending = "pending";
		public const string CiStatusError = "error";
		public const string CiStatusFailure = "failure";
		public const string CiUrlBase = "https://splamy.de/api/qint/log/";
		public static readonly Regex fileCleanRegex = new(@"[^\w\d]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);

		public QintController(StoreService store)
		{
			this.store = store;
		}

		[HttpPost("push")]
		public async Task Push([FromBody] GhPushEvent ev)
		{
			//var expect_secret = await store.Get("build_qint_webhook_secret");
			//if (ev?.hook?.config?.secret != expect_secret) return;

			// TODO renable after testing
			// if (ev?.@ref != "refs/heads/master") { Log.Info("Invalid branch"); return; }

			if (ev?.repository?.full_name != "ReSpeak/Qint") { Log.Info("Invalid project"); return; }
			var commit = ev?.after;
			if (commit is null) { Log.Info("No commit in webhook?"); return; }

			var createBuild = new BuildTask();
			var oldBuild = Interlocked.Exchange(ref currentBuild, createBuild);
			if (oldBuild != null)
			{
				oldBuild.Cts.Cancel();
				await oldBuild.Complete.Task;
			}

			Run(commit, createBuild);
		}

		public async void Run(string commit, BuildTask build)
		{
			var logFileName = fileCleanRegex.Replace($"{commit}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}", "");
			var ciBuildUrl = $"{CiUrlBase}/{logFileName}";

			try
			{
				using var cts = build.Cts;
				await PushJson(new StateBody(CiStatusPending, CiContext, CiDescription, ciBuildUrl), commit, cts.Token);

				System.IO.File.WriteAllLines(build_qint_env, new[] {
					$"QINT_SHA={commit}",
					$"QINT_LOG_FILE={logFileName}",
				});

				await Cli.Wrap("systemctl")
					.WithArguments("start buildqint")
					.ExecuteAsync(cts.Token);

				await PushJson(new StateBody(CiStatusSuccess, CiContext, CiDescription, ciBuildUrl), commit);
			}
			catch (OperationCanceledException)
			{
				await Cli.Wrap("systemctl")
					.WithArguments("kill buildqint")
					.WithValidation(CommandResultValidation.None)
					.ExecuteAsync();

				await PushJson(new StateBody(CiStatusFailure, CiContext, "Cancelled", ciBuildUrl), commit);
			}
			catch (Exception ex)
			{
				Log.Error("Build {0} failed: {1}", commit, ex.Message);
				await PushJson(new StateBody(CiStatusFailure, CiContext, "Failed", ciBuildUrl), commit);
			}
			finally
			{
				Interlocked.CompareExchange(ref currentBuild, null, build);
				build.Complete.TrySetResult();
			}
		}

		[HttpGet("log/{build}")]
		public IActionResult GetLog(string build)
		{
			var logFileName = fileCleanRegex.Replace(build, "");
			if (logFileName.Contains(".") || logFileName.Contains("/") || logFileName.Contains("\\"))
			{
				Log.Fatal("This shouldn't happen");
				return Forbid();
			}

			return File($"/var/lib/buildqint/log/{logFileName}", "text/plain");
		}

		[HttpGet("download")]
		public IActionResult GetBinary()
		{
			return File($"/var/lib/buildqint/out/Qint.zip", "application/x-zip");
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

#pragma warning disable IDE1006 // Naming Styles
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

	public record StateBody(string state, string context, string description, string? target_url);

	public class BuildTask
	{
		public TaskCompletionSource Complete { get; } = new TaskCompletionSource();
		public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
	}
#pragma warning restore IDE1006 // Naming Styles
}
