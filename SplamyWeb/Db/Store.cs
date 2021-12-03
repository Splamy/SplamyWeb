using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SplamyWeb.Db;

[Table("kvp_store")]
public class StoreEntry
{
	[Key]
	public string Id { get; set; }
	public string? Value { get; set; }

	public StoreEntry(string id, string? value)
	{
		Id = id;
		Value = value;
	}
}
