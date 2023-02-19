using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class RamsesService : BackgroundService
	{
		private static readonly Uri BSUri = new("wss://ws.beatsaver.com/maps");
		private readonly ILogger logger;
		private readonly RamsesBackingData ramses;
		private readonly JsonSerializerOptions jsonSerializerOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
		};

		private static ReadOnlySpan<byte> Lf => "\n"u8;

		public RamsesService(ILogger<RamsesService> logger, RamsesBackingData ramses)
		{
			this.logger = logger;
			this.ramses = ramses;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

					var pipe = new Pipe();
					await Task.WhenAny(
						ListenWebsocket(pipe.Writer, cts.Token),
						ReadPipe(pipe.Reader, cts.Token));

					cts.Cancel();
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "");
				}
			}
		}

		private async Task ListenWebsocket(PipeWriter writer, CancellationToken cancellationToken)
		{
			using var ws = new ClientWebSocket();
			await ws.ConnectAsync(BSUri, cancellationToken);
			logger.LogInformation("Ramses websocket connected");

			Memory<byte> buffer = new byte[1024];

			while (!cancellationToken.IsCancellationRequested)
			{
				var result = await ws.ReceiveAsync(buffer, cancellationToken);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					logger.LogInformation("Ramses websocket closed");
					await writer.CompleteAsync();
					return;
				}

				writer.Write(buffer[..result.Count].Span);
				if (result.EndOfMessage)
				{
					writer.Write(Lf);
					await writer.FlushAsync(cancellationToken);
				}
			}
		}

		private async Task ReadPipe(PipeReader reader, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var result = await reader.ReadAsync(cancellationToken);
				if (result.IsCompleted)
				{
					await reader.CompleteAsync();
					break;
				}

				var buffer = result.Buffer;

				while (TryReadLine(ref buffer, out var message))
				{
					logger.LogInformation("New Map Info {MapId}", message.Msg.Id);
					await TriggerEvent(message);
				}

				reader.AdvanceTo(buffer.Start, buffer.End);

				// Stop reading if there's no more data coming.
				if (result.IsCompleted)
				{
					break;
				}
			}
		}

		bool TryReadLine(ref ReadOnlySequence<byte> buffer, out BsMessage message)
		{
			// Look for a EOL in the buffer.
			if (buffer.PositionOf(Lf[0]) is { } position)
			{
				try
				{
					var line = buffer.Slice(0, position);
					var utf8JsonReader = new Utf8JsonReader(line);
					message = JsonSerializer.Deserialize<BsMessage>(ref utf8JsonReader, jsonSerializerOptions);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to read wss message");
					message = default;
				}

				buffer = buffer.Slice(buffer.GetPosition(1, position));
				return true;
			}
			else
			{
				message = default;
				return false;
			}
		}

		private async ValueTask TriggerEvent(BsMessage message)
		{
			if (message.Msg.Versions.Any(x => x.State == "Published"))
			{
				await ramses.Get(message.Msg.Id);
			}
		}

		private readonly struct BsMessage
		{
			public required readonly string Type { get; init; }
			public required readonly BsMessageMsg Msg { get; init; }
		}

		private readonly struct BsMessageMsg
		{
			public required readonly string Id { get; init; }
			public required readonly BsMessageMsgVersions[] Versions { get; init; }
		}

		private readonly struct BsMessageMsgVersions
		{
			public required readonly string State { get; init; }
		}
	}
}
