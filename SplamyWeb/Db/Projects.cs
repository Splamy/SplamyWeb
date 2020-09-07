using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;

namespace SplamyWeb.Db
{
#pragma warning disable CS8618

	[DebuggerDisplay("{Project}: {ProjectName}")]
	[Table("nightly_project")]
	public class NightlyProject
	{
		public ICollection<NightlyBranch> Branches { get; set; }
		public ICollection<LanguageEntry> Languages { get; set; }
		[Key]
		public string Project { get; set; } // Something like "ts3ab", "ts3hook"
		public string ProjectName { get; set; }
		public string CommitUrl { get; set; } // https://github.com/Splamy/TS3AudioBot/commit/{0}
	}

	[DebuggerDisplay("{Project},{Branch}: Active:{Active}")]
	[Table("nightly_branch")]
	public class NightlyBranch
	{
		public NightlyProject NightlyProject { get; set; }
		public ICollection<NightlyBuild> Builds { get; set; }
		public string Project { get; set; }
		public string Branch { get; set; }
		public string? Active { get; set; }
	}

	[Table("nightly_build")]
	[DebuggerDisplay("{NightlyBranch?.Project},{Branch},{Commit}: {Version}")]
	public class NightlyBuild
	{
		public NightlyBranch NightlyBranch { get; set; }
		public string Project { get; set; }
		public string Branch { get; set; }
		public string Commit { get; set; }
		public string Version { get; set; }

		public bool ZipContent { get; set; }
		public string FileName { get; set; }
		public DateTime UploadTime { get; set; }
		public int DownloadCount { get; set; }

		public object Strip() => new
		{
			Project,
			Branch,
			Version,
			Commit,
		};
	}

	[Table("nightly_lang")]
	public class LanguageEntry
	{
		public NightlyProject NightlyProject { get; set; }
		public string Project { get; set; }
		public string Language { get; set; }

		public DateTime UploadTime { get; set; }
		public int DownloadCount { get; set; }

		public CultureInfo GetCulture() => CultureInfo.GetCultureInfo(Language);
	}
#pragma warning restore CS8618
}
