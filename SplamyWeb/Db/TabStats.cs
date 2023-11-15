using Riok.Mapperly.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SplamyWeb.Db;

// JSON

public class TabStatsData
{
	// Meta
	public string? BotVersion { get; set; }
	public string? Platform { get; set; }
	public string? Runtime { get; set; }
	public uint? RunningBots { get; set; }

	// StatsData
	public TimeSpan? TotalUptime { get; set; }
	public TimeSpan? BotsRuntime { get; set; }
	public Dictionary<string, TabStatsFactory>? SongStats { get; set; }

	public uint? CommandCalls { get; set; }
	///<summary>How many actually were started by a user (and not i.e. by event)</summary>
	public uint? CommandFromUser { get; set; }
	public uint? CommandFromApi { get; set; }
}

public class TabStatsFactory
{
	public uint? PlayRequests { get; set; }
	public uint? PlaySucessful { get; set; }
	///<summary>How many actually were started by a user (and not i.e. from a playlist)</summary>
	public uint? PlayFromUser { get; set; }
	public uint? SearchRequests { get; set; }
	public TimeSpan? Playtime { get; set; }
}

// DB

[Table("tabstats_entry")]
public class TabStatsPingDto
{
	[Key]
	public long Id { get; set; }
	public DateTime Time { get; set; }

	// Meta
	public string? BotVersion { get; set; }
	public string? Platform { get; set; }
	public string? Runtime { get; set; }
	public long RunningBots { get; set; }

	// StatsData
	public TimeSpan TotalUptime { get; set; }
	public TimeSpan BotsRuntime { get; set; }
	public List<TabStatsFactoryDto> SongStats { get; set; } = new();

	public long CommandCalls { get; set; }
	///<summary>How many actually were started by a user (and not i.e. by event)</summary>
	public long CommandFromUser { get; set; }
	public long CommandFromApi { get; set; }
}

[Table("tabstats_factory")]
public class TabStatsFactoryDto
{
	public long TabStatsId { get; set; }
	public TabStatsPingDto TabStatsEntry { get; set; } = null!;
	public string FactoryName { get; set; } = null!;

	public long PlayRequests { get; set; }
	public long PlaySucessful { get; set; }
	///<summary>How many actually were started by a user (and not i.e. from a playlist)</summary>
	public long PlayFromUser { get; set; }
	public long SearchRequests { get; set; }
	public TimeSpan Playtime { get; set; }
}

[Mapper]
public static partial class TabStatsMapper
{
	[MapperIgnoreTarget(nameof(TabStatsPingDto.Id))]
	[MapperIgnoreTarget(nameof(TabStatsPingDto.Time))]
	public static partial TabStatsPingDto ToDto(TabStatsData obj);

	[MapperIgnoreTarget(nameof(TabStatsFactoryDto.TabStatsId))]
	[MapperIgnoreTarget(nameof(TabStatsFactoryDto.TabStatsEntry))]
	[MapperIgnoreTarget(nameof(TabStatsFactoryDto.FactoryName))]
	private static partial TabStatsFactoryDto ToDto(TabStatsFactory obj);

	public static TabStatsFactoryDto FlatToDto(KeyValuePair<string, TabStatsFactory> obj)
	{
		var dto = ToDto(obj.Value);
		dto.FactoryName = obj.Key;
		return dto;
	}
}
