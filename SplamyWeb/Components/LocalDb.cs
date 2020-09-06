using AutoMapper;
using LiteDB;
using SplamyWeb.OldDb;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class LocalDb
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		public LiteDatabase Database { get; }
		public ILiteCollection<NightlyEntry> NightlyTable { get; }
		public ILiteCollection<NightlyMeta> NightlyMetaTable { get; }
		public ILiteCollection<NightlyProject> NightlyProjectTable { get; }
		public ILiteCollection<LanguageEntry> LanguageTable { get; }
		public ILiteCollection<LoginData> LoginTable { get; }
		private ILiteCollection<RamsesEntry> RamsesTable { get; } // DONE
		private ILiteCollection<TabStatsEntry> TabStatsTable { get; } // DONE
		private ILiteCollection<Db.StoreEntry> StoreTable { get; } // DONE
		public static string DataPath { get; } = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data"));

		public LocalDb()
		{
			Directory.CreateDirectory(DataPath);
			Database = new LiteDatabase(new ConnectionString()
			{
				Filename = Path.Combine(DataPath, "webdata.litedb"),
				Upgrade = true
			});
			if (Database.CheckpointSize == 0)
			{
				Database.CheckpointSize = 1000;
			}

			NightlyTable = Database.GetCollection<NightlyEntry>();
			NightlyTable.EnsureIndex(x => x.Id, true);
			NightlyTable.EnsureIndex(x => x.Project);
			NightlyTable.EnsureIndex(x => x.Branch);
			NightlyTable.UpdateMany("{ Version: '<?>' }", "Version = null");
			NightlyTable.UpdateMany("{ DownloadCount: 0 }", "DownloadCount = null");

			NightlyMetaTable = Database.GetCollection<NightlyMeta>();
			NightlyMetaTable.EnsureIndex(x => x.Id, true);
			NightlyMetaTable.EnsureIndex(x => x.Project);

			NightlyProjectTable = Database.GetCollection<NightlyProject>();
			NightlyProjectTable.EnsureIndex(x => x.Id, true);

			LanguageTable = Database.GetCollection<LanguageEntry>();
			LanguageTable.EnsureIndex(x => x.Id, true);

			LoginTable = Database.GetCollection<LoginData>();
			LoginTable.EnsureIndex(x => x.Id, true);
			LoginTable.EnsureIndex(x => x.Token, true);
			LoginTable.EnsureIndex(x => x.NameNormalized, true);

			RamsesTable = Database.GetCollection<RamsesEntry>();
			RamsesTable.EnsureIndex(x => x.Id, true);

			TabStatsTable = Database.GetCollection<TabStatsEntry>();
			TabStatsTable.EnsureIndex(x => x.Id, true);

			StoreTable = Database.GetCollection<Db.StoreEntry>();
			StoreTable.EnsureIndex(x => x.Id, true);

			//if (LoginTable.Count() == 0)
			//{
			//	string initToken = RandomToken();
			//	string initPass = RandomToken(16);
			//	var (password, salt) = HashPw(initPass);

			//	LoginTable.Insert(new LoginData(
			//		name: "Splamy",
			//		password: password,
			//		salt: salt,
			//		token: initToken,
			//		rank: UserType.Admin
			//	));
			//	Console.WriteLine("Initial token (written to token.tmp): {0}", initToken);
			//	File.WriteAllText(Path.Combine(DataPath, "token.tmp"), initToken + "\n" + initPass);
			//}
		}

		public async Task Initialize(Db.SplamyContext context, IMapper mapper)
		{
			//await context.Database.EnsureDeletedAsync();
			if (await context.Database.EnsureCreatedAsync())
			{
				Log.Info("Created DB, updating from old");
				await DbUpgrade.DoRamses(context, RamsesTable);
				await DbUpgrade.DoStore(context, StoreTable);
				await DbUpgrade.DoTabStats(context, mapper, TabStatsTable);
				await DbUpgrade.DoUserLogin(context, mapper, LoginTable);

				await context.SaveChangesAsync();
			}
		}

		public void Dispose()
		{
		}

		public void CloseDb()
		{
			Database.Dispose();
		}
	}

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
		public string Version { get; set; }
		public string Commit { get; set; }

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
}
