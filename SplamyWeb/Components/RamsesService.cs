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
	public static readonly JsonSerializerOptions JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
	};

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

		writer.Write("["u8);

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
				writer.Write(","u8);
				await writer.FlushAsync(cancellationToken);
			}
		}

		writer.Write("]"u8);
		await writer.CompleteAsync();
	}

	private async Task ReadPipe(PipeReader reader, CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var message in JsonSerializer.DeserializeAsyncEnumerable<BsMessageBase>(reader.AsStream(), JsonSerializerOptions, cancellationToken))
			{
				await TriggerEvent(message);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "");
		}
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
	public class BsMessageBase
	{
		public string? type { get; init; }
	}

	public class BsMessageMapDelete : BsMessageBase
	{
		[JsonPropertyName("msg")]
		public required string Map { get; init; }
	}

	public class BsMessageMapUpdate : BsMessageBase
	{
		public required MsgUpdate Msg { get; init; }
	}

	public readonly struct MsgUpdate
	{
		public required readonly string Id { get; init; }
		public required readonly BsMessageMsgVersions[] Versions { get; init; }
	}

	public readonly struct BsMessageMsgVersions
	{
		public required readonly string State { get; init; }
	}
}
