using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SplamyWeb.Db;

[Table("kvp_store")]
public class StoreEntry(string id, string value)
{
	[Key]
	public string Id { get; set; } = id;
	public string Value { get; set; } = value;
}
