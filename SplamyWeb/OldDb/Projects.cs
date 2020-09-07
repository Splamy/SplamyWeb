using AutoMapper;
using System;
using System.Globalization;

namespace SplamyWeb.OldDb
{
#pragma warning disable CS8618

	public class NightlyProject
	{
		public string Id { get; set; } // Something like "ts3ab", "ts3hook"
		public string ProjectName { get; set; }
		public string CommitUrl { get; set; } // https://github.com/Splamy/TS3AudioBot/commit/{0}
	}

	public class NightlyEntry
	{
		public string Id => GetId(Project, Branch, Commit);
		public string Project { get; set; }
		public string Branch { get; set; }
		public string Commit { get; set; }
		public string Version { get; set; }

		public bool ZipContent { get; set; }
		public string FileName { get; set; }
		public DateTime UploadTime { get; set; }
		public uint DownloadCount { get; set; }

		public object Strip() => new
		{
			Project,
			Branch,
			Version,
			Commit,
		};

		public static string GetId(string project, string branch, string commit) => $"{project}.{branch}.{commit}";
	}

	public class NightlyMeta
	{
		public string Id { get => GetId(Project, Branch); }
		public string Project { get; set; }
		public string Branch { get; set; }
		public string Active { get; set; }

		public string ToEntryId() => NightlyEntry.GetId(Project, Branch, Active);

		public static string GetId(string project, string branch) => $"{project}.{branch}";
	}

	public class LanguageEntry
	{
		public string Id { get; set; }
		public string Project { get; set; }
		public string Language { get; set; }

		public DateTime UploadTime { get; set; }
		public int DownloadCount { get; set; }

		public CultureInfo GetCulture() => CultureInfo.GetCultureInfo(Language);
	}
#pragma warning restore CS8618

	public class ProjectsProfile : Profile
	{
		public ProjectsProfile()
		{
			CreateMap<NightlyProject, Db.NightlyProject>()
				.ForMember(dst => dst.Project, opt => opt.MapFrom(src => src.Id))
				.ForMember(dst => dst.Branches, opt => opt.Ignore())
				.ForMember(dst => dst.Languages, opt => opt.Ignore());
			CreateMap<NightlyMeta, Db.NightlyBranch>()
				.ForMember(dst => dst.Builds, opt => opt.Ignore())
				.ForMember(dst => dst.NightlyProject, opt => opt.Ignore());
			CreateMap<NightlyEntry, Db.NightlyBuild>()
				.ForMember(dst => dst.NightlyBranch, opt => opt.Ignore());
			CreateMap<LanguageEntry, Db.LanguageEntry>()
				.ForMember(dst => dst.NightlyProject, opt => opt.Ignore());
		}
	}
}
