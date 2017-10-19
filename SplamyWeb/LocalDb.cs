using LiteDB;
using System;
using System.IO;
using System.Text;

namespace SplamyWeb
{
	public static class LocalDb
	{
		private const int TokenLength = 64;

		public static LiteDatabase Database { get; }
		public static LiteCollection<NightlyEntry> NightlyTable { get; }
		public static LiteCollection<LoginData> LoginTable { get; }
		public static string DataPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "data");

		static LocalDb()
		{
			Directory.CreateDirectory(DataPath);
			Database = new LiteDatabase(Path.Combine(DataPath, "webdata.litedb"));
			NightlyTable = Database.GetCollection<NightlyEntry>();
			LoginTable = Database.GetCollection<LoginData>();
			LoginTable.EnsureIndex(x => x.Token, true);

			if (LoginTable.Count() == 0)
			{
				string initToken = RandomToken();
				LoginTable.Insert(new LoginData
				{
					UserName = "Splamy",
					Password = null,
					Token = initToken,
					Rank = UserType.Admin,
				});
				Console.WriteLine("Initial token (written to token.tmp): {0}", initToken);
				File.WriteAllText(Path.Combine(DataPath, "token.tmp"), initToken);
			}
		}

		public static LoginData GetUserByToken(string token)
		{
			if (token == null)
				return null;
			return LoginTable.FindOne(x => x.Token == token);
		}

		private static string RandomToken()
		{
			const string tokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
			var buffer = new byte[TokenLength];
			rng.GetBytes(buffer);
			var strb = new StringBuilder(buffer.Length);
			for (int i = 0; i < buffer.Length; i++)
				strb.Append(tokenChars[(tokenChars.Length * buffer[i]) / 255]);
			return strb.ToString();
		}
	}

	public class NightlyEntry
	{
		[BsonId]
		public string Id { get; set; }
		public string Project { get; set; }
		public string Branch { get; set; }
		public string Version { get; set; }
		public string Commit { get; set; }
		public bool ZipContent { get; set; }
		public string FileName { get; set; }
	}

	public class LoginData
	{
		[BsonId]
		public string UserName { get; set; }
		public string Password { get; set; }
		public string Token { get; set; }
		public UserType Rank { get; set; }
	}

	public enum UserType
	{
		Admin,
		CoAdmin,
		User,
	}
}
