using Math2D;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public sealed class MinigameServer(IHubContext<Minigame, IMinigame> hub)
	{
		private readonly object _lock = new();
		private readonly Random random = new();
		private readonly Dictionary<string, MinigamePlayer> ConnectedPlayer = [];
		private readonly List<MinigameCookie> Cookies = [];
		private readonly Stack<MinigameCookie> CookiePool = new(Enumerable.Range(0, MaxCookies).Select(i => new MinigameCookie(i)));
		public static readonly string[] PlayerNames = ["Aardvark", "Albatross", "Alligator", "Alpaca", "Ant", "Anteater", "Antelope", "Ape", "Armadillo", "Donkey", "Baboon", "Badger", "Barracuda", "Bat", "Bear", "Beaver", "Bee", "Bison", "Boar", "Buffalo", "Butterfly", "Camel", "Capybara", "Caribou", "Cassowary", "Cat", "Caterpillar", "Cattle", "Chamois", "Cheetah", "Chicken", "Chimpanzee", "Chinchilla", "Chough", "Clam", "Cobra", "Cockroach", "Cod", "Cormorant", "Coyote", "Crab", "Crane", "Crocodile", "Crow", "Curlew", "Deer", "Dinosaur", "Dog", "Dogfish", "Dolphin", "Dotterel", "Dove", "Dragonfly", "Duck", "Dugong", "Dunlin", "Eagle", "Echidna", "Eel", "Eland", "Elephant", "Elk", "Emu", "Falcon", "Ferret", "Finch", "Fish", "Flamingo", "Fly", "Fox", "Frog", "Gaur", "Gazelle", "Gerbil", "Giraffe", "Gnat", "Gnu", "Goat", "Goldfinch", "Goldfish", "Goose", "Gorilla", "Goshawk", "Grasshopper", "Grouse", "Guanaco", "Gull", "Hamster", "Hare", "Hawk", "Hedgehog", "Heron", "Herring", "Hippopotamus", "Hornet", "Horse", "Human", "Hummingbird", "Hyena", "Ibex", "Ibis", "Jackal", "Jaguar", "Jay", "Jellyfish", "Kangaroo", "Kingfisher", "Koala", "Kookabura", "Kouprey", "Kudu", "Lapwing", "Lark", "Lemur", "Leopard", "Lion", "Llama", "Lobster", "Locust", "Loris", "Louse", "Lyrebird", "Magpie", "Mallard", "Manatee", "Mandrill", "Mantis", "Marten", "Meerkat", "Mink", "Mole", "Mongoose", "Monkey", "Moose", "Mosquito", "Mouse", "Mule", "Narwhal", "Newt", "Nightingale", "Octopus", "Okapi", "Opossum", "Oryx", "Ostrich", "Otter", "Owl", "Oyster", "Panther", "Parrot", "Partridge", "Peafowl", "Pelican", "Penguin", "Pheasant", "Pig", "Pigeon", "Pony", "Porcupine", "Porpoise", "Quail", "Quelea", "Quetzal", "Rabbit", "Raccoon", "Rail", "Ram", "Rat", "Raven", "Red deer", "Red panda", "Reindeer", "Rhinoceros", "Rook", "Salamander", "Salmon", "Sand Dollar", "Sandpiper", "Sardine", "Scorpion", "Seahorse", "Seal", "Shark", "Sheep", "Shrew", "Skunk", "Snail", "Snake", "Sparrow", "Spider", "Spoonbill", "Squid", "Squirrel", "Starling", "Stingray", "Stinkbug", "Stork", "Swallow", "Swan", "Tapir", "Tarsier", "Termite", "Tiger", "Toad", "Trout", "Turkey", "Turtle", "Viper", "Vulture", "Wallaby", "Walrus", "Wasp", "Weasel", "Whale", "Wildcat", "Wolf", "Wolverine", "Wombat", "Woodcock", "Woodpecker", "Worm", "Wren", "Yak", "Zebra"];
		// Buffers
		private readonly List<MinigamePlayer> updatePlayer = [];
		private IEnumerable<MinigamePlayerState> UpdatePlayerState => updatePlayer;
		private readonly List<MinigameCookie> updateCookies = [];
		private bool fullUpdate;
		// Timings
		private readonly Stopwatch time = new();
		private Timer? timer;
		private static readonly TimeSpan TickWait = TimeSpan.FromSeconds(1 / 64d);
		private uint CurrentTick;
		// Game Rules
		public static readonly Vector2 FieldSize = new(1600, 900);
		public static readonly int CollectRadius = 13;
		private const float TURN_SPEED = 1 / 600f;
		private const float SPEED = 1 / 3f;
		private const int MaxCookies = 10;
		private const uint SyncPlayerAfterMaxTicks = 10;
		private const uint SpawnCookieEachTicks = 200;
		// Game Status
		private uint LastTickCookieSpanwed;

		public void AddClient(string id)
		{
			lock (_lock)
			{
				if (ConnectedPlayer.Count == 0 || timer is null)
				{
					timer = new Timer(GameTick, null, TimeSpan.Zero, TickWait);
				}

				var player = new MinigamePlayer(id, PlayerNames[random.Next(0, PlayerNames.Length)])
				{
					Color = random.Next(0, 360),
				};
				ConnectedPlayer.Add(id, player);
				SyncStateTo(hub.Clients.Client(id));
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

					ResetGame();
				}
			}
		}

		public void SetTarget(string id, Vector2 target)
		{
			if (ConnectedPlayer.TryGetValue(id, out var player))
			{
				player.Target = target;
				player.LastSyncTick = 0;
			}
		}

		private void GameTick(object? state)
		{
			if (ConnectedPlayer.Count == 0)
				return;

			lock (_lock)
			{
				if (ConnectedPlayer.Count == 0)
					return;

				var elapsed = (float)time.Elapsed.TotalMilliseconds;
				time.Restart();
				CurrentTick = unchecked(CurrentTick + 1);

				if (CurrentTick > LastTickCookieSpanwed + SpawnCookieEachTicks)
				{
					SpawnCookie();
				}

				foreach (var player in ConnectedPlayer.Values)
				{
					if (CurrentTick > player.LastSyncTick + SyncPlayerAfterMaxTicks)
					{
						updatePlayer.Add(player);
						player.LastSyncTick = CurrentTick;
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

					CheckCollectCookie(player);
				}
			}

			if (updatePlayer.Count > 0)
			{
				if (fullUpdate)
					hub.Clients.All.PlayersUpdate(updatePlayer);
				else
					hub.Clients.All.PlayersUpdateState(UpdatePlayerState);
				fullUpdate = false;
				updatePlayer.Clear();
			}

			if (updateCookies.Count > 0)
			{
				hub.Clients.All.CookiesUpdate(updateCookies);
				updateCookies.Clear();
			}
		}

		private void SpawnCookie()
		{
			LastTickCookieSpanwed = CurrentTick;
			if (Cookies.Count >= MaxCookies || CookiePool.Count == 0)
				return;

			var cookie = CookiePool.Pop();
			cookie.SetRandom();
			cookie.Active = true;
			updateCookies.Add(cookie);

			for (int i = 0; i < Cookies.Count; i++)
			{
				if (cookie.PartitionX() <= Cookies[i].PartitionX())
				{
					Cookies.Insert(i, cookie);
					return;
				}
			}
			Cookies.Add(cookie);
		}

		private void CheckCollectCookie(MinigamePlayer player)
		{
			var playerX = (int)player.Position.X;
			var checkMinX = playerX - CollectRadius;
			var checkMaxX = playerX + CollectRadius;

			for (int i = 0; i < Cookies.Count; i++)
			{
				var cookie = Cookies[i];
				if (cookie.PartitionX() < checkMinX) continue;
				if (cookie.PartitionX() > checkMaxX) break;

				if (player.Position.Distance(cookie.Position) < CollectRadius)
				{
					Cookies.RemoveAt(i);
					i--;
					CookiePool.Push(cookie);
					cookie.Active = false;
					updateCookies.Add(cookie);

					player.Points++;
					fullUpdate = true;
				}
			}
		}

		private void SyncStateTo(IMinigame client)
		{
			client.InitState(new(ConnectedPlayer.Values, Cookies));
		}

		private void ResetGame()
		{
			CurrentTick = 0;
			LastTickCookieSpanwed = 0;
			Cookies.ForEach(cookie => CookiePool.Push(cookie));
			Cookies.Clear();
		}

		private static float MathMod(float n, float m) => ((n % m) + m) % m;
	}

	public class MinigamePlayerState(string id)
	{
		public string Id { get; } = id;
		public Vector2 Position { get; set; }
		public Vector2 Target { get; set; }
		public float Angle { get; set; }

		public uint LastSyncTick;
	}

	public class MinigamePlayer(string id, string name) : MinigamePlayerState(id)
	{
		public string Name { get; set; } = name;
		public int Color { get; set; }
		public int Points { get; set; }
	}

	public class MinigameCookie(int id)
	{
		private int positionX;
		public int Id { get; set; } = id;
		public Vector2 Position { get; private set; } = Vector2.Zero;
		public bool Active { get; set; }

		public void SetRandom()
		{
			Position = new(
				Random.Shared.NextSingle() * MinigameServer.FieldSize.X,
				Random.Shared.NextSingle() * MinigameServer.FieldSize.Y);
			positionX = (int)Position.X;
		}

		public int PartitionX() => positionX;

		public override string ToString() => $"{(Active ? "" : "-")} {Id} {Position}";

		//private sealed class PartitionXComparer : IComparer<MinigameCookie>
		//{
		//	public static readonly PartitionXComparer Instance = new();
		//	public int Compare(MinigameCookie x, MinigameCookie y)
		//	{
		//		return y.positionX - x.positionX;
		//	}
		//}
	}

	public class MinigameState(ICollection<MinigamePlayer> players, ICollection<MinigameCookie> collectibles)
	{
		public ICollection<MinigamePlayer> Players { get; init; } = players;
		public ICollection<MinigameCookie> Collectibles { get; init; } = collectibles;
	}

	public class Minigame(IServiceProvider serviceProvider) : Hub<IMinigame>
	{
		private readonly MinigameServer server = serviceProvider.GetRequiredService<MinigameServer>();

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

	public interface IMinigame
	{
		Task InitState(MinigameState state);
		Task PlayerLeft(string userId);
		Task PlayersUpdateState(IEnumerable<MinigamePlayerState> players);
		Task PlayersUpdate(IEnumerable<MinigamePlayer> players);
		Task CookiesUpdate(IList<MinigameCookie> cookies);
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

#pragma warning disable IDE1006
	record struct Vec2Converter(float x, float y);
#pragma warning restore IDE1006
}
