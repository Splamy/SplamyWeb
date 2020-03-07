using LiteDB;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class LocalDb : IRoleStore<LoginData>, IUserPasswordStore<LoginData>, IPasswordValidator<LoginData>, IPasswordHasher<LoginData>
	{
		public LiteDatabase Database { get; }
		public ILiteCollection<NightlyEntry> NightlyTable { get; }
		public ILiteCollection<NightlyMeta> NightlyMetaTable { get; }
		public ILiteCollection<NightlyProject> NightlyProjectTable { get; }
		public ILiteCollection<LanguageEntry> LanguageTable { get; }
		public ILiteCollection<LoginData> LoginTable { get; }
		public ILiteCollection<RamsesEntry> RamsesTable { get; }
		public ILiteCollection<TabStatsEntry> TabStatsTable { get; }
		public static string DataPath { get; } = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data"));

		public LocalDb()
		{
			Directory.CreateDirectory(DataPath);
			Database = new LiteDatabase(new ConnectionString()
			{
				Filename = Path.Combine(DataPath, "webdata.litedb"),
				Upgrade = true
			});
			NightlyTable = Database.GetCollection<NightlyEntry>();
			NightlyTable.EnsureIndex(x => x.Id, true);
			NightlyTable.EnsureIndex(x => x.Project);
			NightlyTable.EnsureIndex(x => x.Branch);

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

			if (LoginTable.Count() == 0)
			{
				string initToken = RandomToken();
				string initPass = RandomToken(16);
				var (password, salt) = HashPw(initPass);

				LoginTable.Insert(new LoginData(
					name: "Splamy",
					password: password,
					salt: salt,
					token: initToken,
					rank: UserType.Admin
				));
				Console.WriteLine("Initial token (written to token.tmp): {0}", initToken);
				File.WriteAllText(Path.Combine(DataPath, "token.tmp"), initToken + "\n" + initPass);
			}
			else
			{
				var oldEntries = LoginTable.Find(x => x.NameNormalized == null);
				foreach (var e in oldEntries)
				{
					e.SetName(e.Name);
					LoginTable.Update(e);
				}
			}
		}

		public LoginData? GetUserByToken(string token)
		{
			if (token == null)
				return null;
			return LoginTable.FindOne(x => x.Token == token);
		}

		private static string RandomToken(int length = 64)
		{
			const string tokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			using var rng = RandomNumberGenerator.Create();
			var buffer = new byte[length];
			rng.GetBytes(buffer);
			var strb = new StringBuilder(buffer.Length);
			for (int i = 0; i < buffer.Length; i++)
				strb.Append(tokenChars[(tokenChars.Length - 1) * buffer[i] / 255]);
			return strb.ToString();
		}

		public static (string password, byte[] salt) HashPw(string password)
		{
			// generate a 128-bit salt using a secure PRNG
			var salt = new byte[128 / 8];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}
			return (HashPw(password, salt), salt);
		}

		public static string HashPw(string password, byte[] salt)
		{
			// derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
			return Convert.ToBase64String(KeyDerivation.Pbkdf2(
				password: password,
				salt: salt,
				prf: KeyDerivationPrf.HMACSHA256,
				iterationCount: 10000,
				numBytesRequested: 256 / 8));
		}

		public void Dispose()
		{
			//Database.Dispose();
		}

		#region Never go full enterprise

#pragma warning disable CS1998
		public async Task<string> GetUserIdAsync(LoginData user, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return user.Id.ToString(CultureInfo.InvariantCulture);
		}

		public async Task<string> GetUserNameAsync(LoginData user, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return user.Name;
		}

		public async Task SetUserNameAsync(LoginData user, string userName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			user.Name = userName;
			LoginTable.Update(user);
		}

		public async Task<string> GetNormalizedUserNameAsync(LoginData user, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return user.NameNormalized;
		}

		public async Task SetNormalizedUserNameAsync(LoginData user, string normalizedName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			user.NameNormalized = normalizedName;
			LoginTable.Update(user);
		}

		public async Task<IdentityResult> CreateAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				LoginTable.Insert(role);
				return IdentityResult.Success;
			}
			catch { return IdentityResult.Failed(new IdentityError { Code = "UserAlreadyExists", Description = "Could not create because user already exists" }); }
		}

		public async Task<IdentityResult> UpdateAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return LoginTable.Update(role)
				? IdentityResult.Success
				: IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "The user to update could not be found" });
		}

		public async Task<IdentityResult> DeleteAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return LoginTable.Delete(role.Id)
				? IdentityResult.Success
				: IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "The user to delete could not be found" });
		}

		public async Task<string> GetRoleIdAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return role.Rank.ToString().ToUpperInvariant();
		}

		public async Task<string> GetRoleNameAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return role.Rank.ToString();
		}

		public async Task SetRoleNameAsync(LoginData role, string roleName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			role.Rank = Enum.Parse<UserType>(roleName);
		}

		public async Task<string> GetNormalizedRoleNameAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return role.Rank.ToString().ToUpperInvariant();
		}

		public async Task SetNormalizedRoleNameAsync(LoginData role, string normalizedName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			role.Rank = Enum.Parse<UserType>(normalizedName, true);
			LoginTable.Update(role);
		}

		public async Task<LoginData> FindByIdAsync(string roleId, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return LoginTable.FindById(int.Parse(roleId, CultureInfo.InvariantCulture));
		}

		public async Task<LoginData> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var user = LoginTable.FindOne(x => x.NameNormalized == normalizedRoleName);
			return user;
		}

		public async Task SetPasswordHashAsync(LoginData user, string passwordHash, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			user.Password = passwordHash;
			LoginTable.Update(user);
		}

		public async Task<string> GetPasswordHashAsync(LoginData user, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return user.Password;
		}

		public async Task<bool> HasPasswordAsync(LoginData user, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return user.Password != null;
		}

		public async Task<IdentityResult> ValidateAsync(UserManager<LoginData> manager, LoginData user, string password)
		{
			return IdentityResult.Success;
		}

		public string HashPassword(LoginData user, string password)
		{
			return HashPw(password, user.Salt);
		}

		public PasswordVerificationResult VerifyHashedPassword(LoginData user, string hashedPassword, string providedPassword)
		{
			if (HashPw(providedPassword, user.Salt) == hashedPassword)
			{
				return PasswordVerificationResult.Success;
			}
			return PasswordVerificationResult.Failed;
		}
#pragma warning restore CS1998

		#endregion
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
		public int DownloadCount { get; set; }

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

	public class LoginData
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string NameNormalized { get; set; }
		public string Password { get; set; }
		public byte[] Salt { get; set; }
		public string Token { get; set; }
		public UserType Rank { get; set; }

		[Obsolete("Reserved for DB", true)]
		public LoginData() { }

		public void SetName(string name)
		{
			this.Name = name;
			this.NameNormalized = NormalizeName(name);
		}

		public LoginData(string name, string password, byte[] salt, string token, UserType rank)
		{
			Name = name;
			NameNormalized = NormalizeName(name);
			Password = password;
			Salt = salt;
			Token = token;
			Rank = rank;
		}

		public static string NormalizeName(string name) => name.ToUpperInvariant();

		public bool CanEditOtherUser() => Rank.AtLeast(UserType.Admin);
		public bool CanSetRankUpTo(UserType targetRank) => targetRank <= Rank.CanSetRankUpTo();
	}

	// Rank (currently) ordered by number
	// the lower the more powerful
	// 0 = Admin
	public enum UserType
	{
		Admin,
		User,
	}

	public static class Rank
	{
		public static bool AtLeast(this UserType self, UserType rankOrHigher)
		{
			return self <= rankOrHigher;
		}

		public static UserType? CanSetRankUpTo(this UserType self) => self switch
		{
			UserType.Admin => UserType.Admin,
			_ => null,
		};
	}

#pragma warning restore CS8618
}
