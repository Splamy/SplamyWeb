using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class RamsesService(ILogger<RamsesService> logger, RamsesBackingData ramses) : BackgroundService
{
	private static readonly Uri BSUri = new("wss://ws.beatsaver.com/maps");
	private readonly JsonSerializerOptions jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
	};

	private static ReadOnlySpan<byte> Lf => "\n"u8;

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

				await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
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

			writer.Write(buffer.Span[..result.Count]);
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

	bool TryReadLine(ref ReadOnlySequence<byte> buffer, [MaybeNullWhen(false)] out BsMessageBase message)
	{
		// Look for a EOL in the buffer.
		if (buffer.PositionOf(Lf[0]) is { } position)
		{
			var line = buffer.Slice(0, position);
			buffer = buffer.Slice(buffer.GetPosition(1, position));

			try
			{
				var utf8JsonReader = new Utf8JsonReader(line);
				message = JsonSerializer.Deserialize<BsMessageBase>(ref utf8JsonReader, jsonSerializerOptions) ?? throw new Exception("Message was null");
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to read wss message");
			}
		}

		message = default;
		return false;
	}

	private async ValueTask TriggerEvent(BsMessageBase message)
	{
		switch (message)
		{
		case BsMessageMapUpdate mapUpdate:
			logger.LogInformation("New Map Update {@MapId}", mapUpdate.Msg.Id);
			if (mapUpdate.Msg.Versions.Any(x => x.State == "Published"))
			{
				await ramses.Get(mapUpdate.Msg.Id);
			}
			break;

		case BsMessageMapDelete mapDelete:
			logger.LogInformation("Map Deleted {@MapId}", mapDelete.Map);
			break;

		default:
			logger.LogInformation("BS wss event {@Type}", message.type);
			break;
		}
	}

	[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
	[JsonDerivedType(typeof(BsMessageMapUpdate), typeDiscriminator: "MAP_UPDATE")]
	[JsonDerivedType(typeof(BsMessageMapDelete), typeDiscriminator: "MAP_DELETE")]
	private class BsMessageBase
	{
		public string? type { get; init; }
	}

	private class BsMessageMapDelete : BsMessageBase
	{
		[JsonPropertyName("msg")]
		public required string Map { get; init; }
	}

	private class BsMessageMapUpdate : BsMessageBase
	{
		public required MsgUpdate Msg { get; init; }
	}

	private readonly struct MsgUpdate
	{
		public required readonly string Id { get; init; }
		public required readonly BsMessageMsgVersions[] Versions { get; init; }
	}

	private readonly struct BsMessageMsgVersions
	{
		public required readonly string State { get; init; }
	}
}
