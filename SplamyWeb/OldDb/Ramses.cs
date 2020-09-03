namespace SplamyWeb.OldDb
{
#pragma warning disable CS8618
	public class RamsesEntry
	{
		public string Id { get; set; }
		public string Version { get; set; }
		public RamsesMap[] Maps { get; set; }
	}

	public class RamsesMap
	{
		public string Difficulty { get; set; }
		public float MaxDifficulty { get; set; }
		public float AvgDifficulty { get; set; }
		public float[] Graph { get; set; }
	}
#pragma warning restore CS8618
}
