using RateMapSeveritySaber;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace SplamyWeb.Db
{
	// TODO user automapper

	[Table("ramses_song")]
	[DebuggerDisplay("{Id}")]
	public class RamsesSong
	{
		/// <summary>Hexadecimal beatsaver map id.</summary>
		[Key]
		[JsonIgnore]
		public long Id { get; set; }

		/// <summary>Version of the ramses engine this result was generated with.</summary>
		[JsonPropertyName("ramsesVersion")]
		public string Version { get; set; }
		[JsonPropertyName("maps")]
		public List<RamsesMap> Maps { get; set; } = new();
		[JsonIgnore]
		public byte[]? RawMap { get; set; }

		public RamsesSong(long id, string version, byte[]? rawMap = null)
		{
			Id = id;
			Version = version;
			RawMap = rawMap;
		}
	}

	[Table("ramses_map")]
	public class RamsesMap
	{
		// Notes:
		// Characteristic+Difficulty is not unique!
		[JsonIgnore]
		public long RamsesId { get; set; }
		[JsonIgnore]
		public RamsesSong RamsesEntry { get; set; }
		/// <summary>Index in the _difficultyBeatmapSets array</summary>
		[JsonIgnore]
		public MapCharacteristic Characteristic { get; set; }
		/// <summary>Index in the _difficultyBeatmaps array</summary>
		[JsonIgnore]
		public byte IndexDifficulty { get; set; }

		[JsonIgnore]
		public byte Difficulty { get; set; }
		[NotMapped]
		[JsonPropertyName("difficulty")]
		public string DifficultyName => BSMapUtil.DifficultyNumberToName(Difficulty);

		/// <summary>Internal mode name (Standard, 90°, 360°,...)</summary>
		[NotMapped]
		[JsonPropertyName("characteristic")]
		public string CharacteristicName => BSMapUtil.CharacteristicToName(Characteristic);

		[JsonPropertyName("maxDifficulty")]
		public float MaxDifficulty { get; set; }
		[JsonPropertyName("avgDifficulty")]
		public float AvgDifficulty { get; set; }
		[JsonPropertyName("graph")]
		public float[] Graph { get; set; }

		public RamsesMap(MapCharacteristic characteristic, byte indexDifficulty, byte difficulty, float maxDifficulty, float avgDifficulty, float[] graph)
		{
			RamsesId = 0;
			RamsesEntry = null!;
			Characteristic = characteristic;
			IndexDifficulty = indexDifficulty;
			Difficulty = difficulty;
			MaxDifficulty = maxDifficulty;
			AvgDifficulty = avgDifficulty;
			Graph = graph;
		}
	}
}
