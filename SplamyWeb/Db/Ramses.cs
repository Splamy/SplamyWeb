using RateMapSeveritySaber;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace SplamyWeb.Db
{
	// DB

	[Table("ramses_song")]
	[DebuggerDisplay("{Id} @{Version}")]
	public class RamsesSongDto
	{
		/// <summary>Hexadecimal beatsaver map id.</summary>
		[Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }

		/// <summary>Version of the ramses engine this result was generated with.</summary>
		public string Version { get; set; }
		public List<RamsesMapDto> Maps { get; set; } = new();
		public byte[]? RawMap { get; set; }

		public RamsesSongDto(long id, string version, byte[]? rawMap = null)
		{
			Id = id;
			Version = version;
			RawMap = rawMap;
		}
	}

	[Table("ramses_map")]
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class RamsesMapDto
	{
		// Notes:
		// Characteristic+Difficulty is not unique!
		public long RamsesId { get; set; }
		public RamsesSongDto RamsesEntry { get; set; }
		/// <summary>Index in the _difficultyBeatmapSets array</summary>
		public MapCharacteristic Characteristic { get; set; }
		/// <summary>Index in the _difficultyBeatmaps array</summary>
		public byte IndexDifficulty { get; set; }
		/// <summary>The indicated Difficulty by BeatSaber value</summary>
		public byte Difficulty { get; set; }
		/// <summary>The calculated rating result from RaMSeS in a compressed format</summary>
		public float Rating { get; set; }
		/// <summary>The calculated rating result from RaMSeS in a compressed format</summary>
		public byte[] RatingDetail { get; set; }

		public RamsesMapDto(MapCharacteristic characteristic, byte indexDifficulty, byte difficulty, float rating, byte[] ratingDetail)
		{
			RamsesId = 0;
			RamsesEntry = null!;
			Characteristic = characteristic;
			IndexDifficulty = indexDifficulty;
			Difficulty = difficulty;
			Rating = rating;
			RatingDetail = ratingDetail;
		}

		private string GetDebuggerDisplay() => $"{RamsesId:X}({RamsesId})|{Characteristic}[{IndexDifficulty}] Diff:{Difficulty} Rate:{Rating}";
	}
}
