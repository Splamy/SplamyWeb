using LiteDB;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using SplamyWeb.Controllers;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class LocalDb : IRoleStore<LoginData>, IUserPasswordStore<LoginData>, IPasswordValidator<LoginData>, IPasswordHasher<LoginData>
	{
		private const string NomalizedName = "NameNormal";

		public LiteDatabase Database { get; }
		public LiteCollection<NightlyEntry> NightlyTable { get; }
		public LiteCollection<NightlyMeta> NightlyMetaTable { get; }
		public LiteCollection<NightlyProject> NightlyProjectTable { get; }
		public LiteCollection<LanguageEntry> LanguageTable { get; }
		public LiteCollection<LoginData> LoginTable { get; }
		public static string DataPath { get; } = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data"));

		public LocalDb()
		{
			var mapper = BsonMapper.Global;

			Directory.CreateDirectory(DataPath);
			Database = new LiteDatabase(Path.Combine(DataPath, "webdata.litedb"));
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
			LoginTable.EnsureIndex(NomalizedName, "UPPER($.Name)", true);

			if (LoginTable.Count() == 0)
			{
				string initToken = RandomToken();
				string initPass = RandomToken(16);
				var (password, salt) = HashPw(initPass);

				LoginTable.Insert(new LoginData
				{
					Name = "Splamy",
					Password = password,
					Salt = salt,
					Token = initToken,
					Rank = UserType.Admin,
				});
				Console.WriteLine("Initial token (written to token.tmp): {0}", initToken);
				File.WriteAllText(Path.Combine(DataPath, "token.tmp"), initToken + "\n" + initPass);
			}
		}

		public LoginData GetUserByToken(string token)
		{
			if (token == null)
				return null;
			return LoginTable.FindOne(x => x.Token == token);
		}

		private static string RandomToken(int length = 64)
		{
			const string tokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			using (var rng = RandomNumberGenerator.Create())
			{
				var buffer = new byte[length];
				rng.GetBytes(buffer);
				var strb = new StringBuilder(buffer.Length);
				for (int i = 0; i < buffer.Length; i++)
					strb.Append(tokenChars[(tokenChars.Length - 1) * buffer[i] / 255]);
				return strb.ToString();
			}
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
			return user.Id.ToString();
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
			return user.Name.ToUpper(CultureInfo.InvariantCulture);
		}

		public async Task SetNormalizedUserNameAsync(LoginData user, string normalizedName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			user.Name = normalizedName;
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
			catch { return IdentityResult.Failed(new IdentityError { Code = "it", Description = "failed" }); }
		}

		public async Task<IdentityResult> UpdateAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				LoginTable.Update(role);
				return IdentityResult.Success;
			}
			catch { return IdentityResult.Failed(new IdentityError { Code = "it", Description = "failed" }); }
		}

		public async Task<IdentityResult> DeleteAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				LoginTable.Delete(role.Id);
				return IdentityResult.Success;
			}
			catch { return IdentityResult.Failed(new IdentityError { Code = "it", Description = "failed" }); }
		}

		public async Task<string> GetRoleIdAsync(LoginData role, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return role.Rank.ToString().ToUpper();
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
			return role.Rank.ToString().ToUpper(CultureInfo.InvariantCulture);
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
			return LoginTable.FindById(int.Parse(roleId));
		}

		public async Task<LoginData> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var user = LoginTable.FindOne(Query.EQ(NomalizedName, normalizedRoleName));
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
			if (HashPw(password, user.Salt) == user.Password)
				return IdentityResult.Success;
			else
				return IdentityResult.Failed(new IdentityError { Code = "it", Description = "failed" });
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

	public class NightlyProject
	{
		public string Id { get; set; } // Something like "ts3ab", "ts3hook"
		public string ProjectName { get; set; }
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
		public string Password { get; set; }
		public byte[] Salt { get; set; }
		public string Token { get; set; }
		public UserType Rank { get; set; }
	}

	public enum UserType
	{
		User,
		Admin,
	}
}
