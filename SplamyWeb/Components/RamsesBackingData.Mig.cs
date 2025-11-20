using JsonBinMin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RateMapSeveritySaber;
using SplamyWeb.Db;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

partial class RamsesBackingData
{
	public async Task MigrateDb(CancellationToken cancellationToken)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();
		await using var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		var upradeMaps = await db.RamsesSongs
			.Where(x => x.JbmVersion != JbmVersion)
			.Select(x => x.Id)
			.OrderBy(x => x)
			.ToListAsync(cancellationToken);

		if (upradeMaps.Count == 0)
			return;

		Log.Info($"Upgrading {upradeMaps.Count} maps to {JbmVersion}");

		foreach (var id in upradeMaps)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			Log.Info($"Upgrading map {id:X}");

			var map = await db.RamsesSongs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
			if (map?.RawMap == null) continue;

			try
			{
				if (map.JbmVersion is "" or "2.0")
				{
					// Re-encode

					var original = map.JbmVersion is "" ? UnpackMapOld(map.RawMap) : UnpackMap(map.RawMap);
					if (original is null)
					{
						Log.Error($"Failed to decode map {id:X}");
						continue;
					}
					var encoded = PackMap(original);

					if (encoded is null)
					{
						Log.Error($"Failed to encode map {id:X}");
						continue;
					}

					// Validate

					var unpacked = UnpackMap(encoded);
					bool hasErrors = false;

					if (!original.Files.ToHashSet(StringComparer.OrdinalIgnoreCase)
						.SequenceEqual(
						unpacked.Files.ToHashSet(StringComparer.OrdinalIgnoreCase)))
					{
						hasErrors = true;
					}

					foreach (var file in original.Files)
					{
						if (!StreamsEqual(original.Get(file), unpacked.Get(file)))
						{
							Log.Error($"Failed to validate encoded map equal {id:X} -> {file}");
							hasErrors = true;
						}
					}

					if (hasErrors)
					{
						Log.Error($"Failed to validate encoded map {id:X}");
						continue;
					}

					// Update

					map.RawMap = encoded;
					map.JbmVersion = "2.1";
				}

				if (map.JbmVersion is "2.1")
				{
					var unpacked = UnpackMap(map.RawMap);
					using var info = unpacked.Get("info.dat");
					if (info is null)
					{
						Log.Error($"Failed to decode map {id:X}");
						continue;
					}

					var json = JsonSerializer.Deserialize<JsonDocument>(info, jbmMapOptions.JsonSerializerOptions);

					if (json is null)
					{
						Log.Error($"Failed to decode map {id:X}");
						continue;
					}

					map.Info = json;

					map.JbmVersion = "2.1.a";
				}

				if (map.JbmVersion != JbmVersion)
				{
					Log.Error($"Failed to upgrade map {id:X}");
					continue;
				}

				await db.SaveChangesAsync(CancellationToken.None);
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Failed to validate encoded map {id:X}");
				continue;
			}
		}
	}

	private static bool StreamsEqual(Stream? a, Stream? b)
	{
		if (a is null && b is null)
			return true;
		if (a is null || b is null)
			return false;
		if (a.Length != b.Length)
			return false;

		var jsonA = JsonSerializer.Deserialize<JsonElement>(a);
		var jsonB = JsonSerializer.Deserialize<JsonElement>(b);

		return AssertStructuralEqual(jsonA, jsonB);
	}

	public static bool AssertStructuralEqual(JsonElement jsonExpected, JsonElement jsonActual)
	{
		if (jsonExpected.ValueKind != jsonActual.ValueKind)
			return false;
		switch (jsonExpected.ValueKind)
		{
		case JsonValueKind.Object:
			var o1 = jsonExpected.EnumerateObject().OrderBy(j => j.Name).ToList();
			var o2 = jsonActual.EnumerateObject().OrderBy(j => j.Name).ToList();
			if (o1.Count != o2.Count)
				return false;
			foreach (var (expected, actual) in o1.Zip(o2))
			{
				if (expected.Name != actual.Name)
					return false;
				if (!AssertStructuralEqual(expected.Value, actual.Value))
					return false;
			}
			return true;
		case JsonValueKind.Array:
			if (jsonExpected.GetArrayLength() != jsonActual.GetArrayLength())
				return false;
			foreach (var (expected, actual) in jsonExpected.EnumerateArray().Zip(jsonActual.EnumerateArray()))
				if (!AssertStructuralEqual(expected, actual))
					return false;
			return true;
		case JsonValueKind.String:
			return jsonExpected.GetString() == jsonActual.GetString();
		case JsonValueKind.Number:
			return jsonExpected.GetRawText() == jsonActual.GetRawText();
		case JsonValueKind.Undefined:
		case JsonValueKind.True:
		case JsonValueKind.False:
		case JsonValueKind.Null:
			return true;
		case var unhandled:
			throw new MissingMemberException("Missing case:" + unhandled.ToString());
		}
	}

	private static byte[] ReadStreamToEnd(Stream stream)
	{
		using var mem = new MemoryStream();
		stream.CopyTo(mem);
		return mem.ToArray();
	}

	private static BsMapProvider UnpackMapOld(byte[] data)
	{
		var output = new MemoryStream();
		using (var input = new MemoryStream(data))
		using (var decompressor = new BrotliStream(input, CompressionMode.Decompress))
		{
			decompressor.CopyTo(output);
		}
		output.Position = 0;
		var intermediateZip = new ZipArchive(output, ZipArchiveMode.Read);
		return new OldCompressedZipProvider(intermediateZip, jbmMapOptions);
	}

	public class OldCompressedZipProvider(ZipArchive zip, JBMOptions? options = null) : BsMapProvider
	{
		public override IEnumerable<string> Files => zip.Entries.Select(e => NormalizeName(e.FullName));
		public override Stream? Get(string file)
		{
			using var mem = new MemoryStream();
			using (var stream = zip.Entries
				.FirstOrDefault((e) => MatchName(e.Name, file))?.Open())
			{
				if (stream is null) return null;
				stream.CopyTo(mem);
			}

			var arr = mem.ToArray();
			if (arr[0] == 0b0_1111_101)
			{
				arr[0] = 0b0000_0011;
			}
			else
			{
				arr = [0b0000_0001, .. arr];
			}


			return JBMConverter.DecodeToStream(arr, options);
		}
	}
}
