using RateMapSeveritySaber;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json;

namespace SplamyWeb.Db;

// DB

[Table("ramses_song")]
[DebuggerDisplay("{Id} R:@{RamsesVersion} J:{JbmVersion} Maps:{Maps.Count}")]
public class RamsesSongDto(
	long id,
	string ramsesVersion,
	string jbmVersion,
	JsonDocument info,
	DateTimeOffset? downloadDate,
	byte[]? rawMap = null)
{
	/// <summary>Hexadecimal beatsaver map id.</summary>
	[Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
	public long Id { get; set; } = id;

	/// <summary>Version of the ramses engine this result was generated with.</summary>
	public string RamsesVersion { get; set; } = ramsesVersion;
	/// <summary>Version of the jbm engine this result was packed with.</summary>
	public string JbmVersion { get; set; } = jbmVersion;

	public List<RamsesMapDto> Maps { get; set; } = [];
	public byte[]? RawMap { get; set; } = rawMap;

	[Column(TypeName = "jsonb")]
	public JsonDocument Info { get; set; } = info;

	public DateTimeOffset? DownloadDate { get; set; } = downloadDate;
}

public class RamsesSongLightDto
{
	/// <inheritdoc cref="RamsesSongDto.RamsesVersion"/>/>
	public required string RamsesVersion { get; set; }
	/// <inheritdoc cref="RamsesSongDto.JbmVersion"/>/>
	public required string JbmVersion { get; set; }
	public List<RamsesMapDto> Maps { get; set; } = [];
}

[Table("ramses_map")]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class RamsesMapDto(MapCharacteristic characteristic, byte indexDifficulty, byte difficulty, float rating, byte[] ratingDetail)
{
	// Notes:
	// Characteristic+Difficulty is not unique!
	public long RamsesId { get; set; }
	public RamsesSongDto RamsesEntry { get; set; } = null!;
	/// <summary>Index in the _difficultyBeatmapSets array</summary>
	public MapCharacteristic Characteristic { get; set; } = characteristic;
	/// <summary>Index in the _difficultyBeatmaps array</summary>
	public byte IndexDifficulty { get; set; } = indexDifficulty;
	/// <summary>The indicated Difficulty by BeatSaber value</summary>
	public byte Difficulty { get; set; } = difficulty;
	/// <summary>The calculated rating result from RaMSeS in a compressed format</summary>
	public float Rating { get; set; } = rating;
	/// <summary>The calculated rating result from RaMSeS in a compressed format</summary>
	public byte[] RatingDetail { get; set; } = ratingDetail;

	private string GetDebuggerDisplay() => $"{RamsesId:X}({RamsesId})|{Characteristic}[{IndexDifficulty}] Diff:{Difficulty} Rate:{Rating}";
}
