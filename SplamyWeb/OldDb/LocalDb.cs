using AutoMapper;
using LiteDB;
using System.IO;
using System.Threading.Tasks;

namespace SplamyWeb.OldDb
{
	public class LocalDb
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		public LiteDatabase Database { get; }
		private ILiteCollection<NightlyEntry> NightlyTable { get; } // DONE
		private ILiteCollection<NightlyMeta> NightlyMetaTable { get; } // DONE
		private ILiteCollection<NightlyProject> NightlyProjectTable { get; } // DONE
		private ILiteCollection<LanguageEntry> LanguageTable { get; } // DONE
		private ILiteCollection<LoginData> LoginTable { get; } // DONE
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
				await DbUpgrade.DoNightly(context, mapper,
					NightlyTable,
					NightlyMetaTable,
					NightlyProjectTable,
					LanguageTable);

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
}
