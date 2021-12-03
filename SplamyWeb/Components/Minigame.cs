using Math2D;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class MinigameServer
	{
		private readonly object _lock = new();
		public Dictionary<string, MiniGamePlayer> ConnectedPlayer = new();
		private readonly IHubContext<Minigame, IMiniGame> hub;
		private Timer? timer;
		private static readonly TimeSpan TickWait = TimeSpan.FromSeconds(1 / 64f);
		private readonly List<MiniGamePlayer> updateList = new();
		private readonly Stopwatch time = new();

		public MinigameServer(IHubContext<Minigame, IMiniGame> hub)
		{
			this.hub = hub;
		}

		public MiniGamePlayer AddClient(string id)
		{
			hub.Clients.All.PlayerJoined(id);
			lock (_lock)
			{
				if (ConnectedPlayer.Count == 0 || timer is null)
				{
					timer = new Timer(GameTick, null, TimeSpan.Zero, TickWait);
				}

				var player = new MiniGamePlayer() { Id = id };
				ConnectedPlayer.Add(id, player);
				return player;
			}
		}

		public void RemoveClient(string id)
		{
			hub.Clients.All.PlayerLeft(id);
			lock (_lock)
			{
				ConnectedPlayer.Remove(id);

				if (ConnectedPlayer.Count == 0 && timer is not null)
				{
					timer.Dispose();
					timer = null;
					time.Stop();
				}
			}
		}

		public void SetTarget(string id, Vector2 target)
		{
			if (ConnectedPlayer.TryGetValue(id, out var player))
			{
				player.Target = target;
				player.TargetSynced = false;
			}
		}

		private void GameTick(object? state)
		{
			var elapsed = (float)time.Elapsed.TotalMilliseconds;
			time.Restart();

			lock (_lock)
			{
				foreach (var player in ConnectedPlayer.Values)
				{
					//if (!player.TargetSynced)
					{
						updateList.Add(player);
						player.TargetSynced = true;
					}

					var diff = player.Position - player.Target;
					var targetAngle = MathF.Atan2(diff.Y, diff.X);
					var angleDiff = targetAngle - player.Angle;
					var angleAdjust = MathMod(angleDiff, MathF.PI * 2) - MathF.PI;
					var angleClamp = MathF.Min(TURN_SPEED, MathF.Max(-TURN_SPEED, angleAdjust));
					player.Angle += angleClamp * elapsed;

					var x = player.Position.X + MathF.Cos(player.Angle) * (elapsed * SPEED);
					var y = player.Position.Y + MathF.Sin(player.Angle) * (elapsed * SPEED);
					player.Position = new(x, y);
				}
			}

			if (updateList.Count > 0)
			{
				hub.Clients.All.PlayersUpdate(updateList);
				updateList.Clear();
			}
		}

		const float TURN_SPEED = 1 / 600f;
		const float SPEED = 1 / 3f;
		private static float MathMod(float n, float m) => ((n % m) + m) % m;
	}

	public class MiniGamePlayer
	{
		public string Id { get; set; }
		public Vector2 Position { get; set; }
		public Vector2 Target { get; set; }
		public float Angle { get; set; }

		public bool TargetSynced { get; set; }
	}

	public class Minigame : Hub<IMiniGame>
	{
		private readonly MinigameServer server;

		public Minigame(IServiceProvider serviceProvider)
		{
			this.server = serviceProvider.GetRequiredService<MinigameServer>();
		}

		public override Task OnConnectedAsync()
		{
			server.AddClient(Context.ConnectionId);
			return base.OnConnectedAsync();
		}

		public override Task OnDisconnectedAsync(Exception? exception)
		{
			server.RemoveClient(Context.ConnectionId);
			return base.OnDisconnectedAsync(exception);
		}

		public void SetTarget(Vector2 target)
		{
			server.SetTarget(Context.ConnectionId, target);
		}
	}

	public interface IMiniGame
	{
		Task PlayerJoined(string userId);
		Task PlayerLeft(string userId);
		Task PlayersUpdate(IList<MiniGamePlayer> players);
	}

	class Vector2Converter : System.Text.Json.Serialization.JsonConverter<Vector2>
	{
		public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var vec2 = JsonSerializer.Deserialize<Vec2Converter>(ref reader);
			return new Vector2(vec2.x, vec2.y);
		}

		public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteNumber("x", value.X);
			writer.WriteNumber("y", value.Y);
			writer.WriteEndObject();
		}
	}

	record struct Vec2Converter(float x, float y);
}
