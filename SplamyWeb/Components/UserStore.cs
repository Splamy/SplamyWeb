using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Db;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public sealed class UserStore : IRoleStore<LoginData>, IUserPasswordStore<LoginData>, IPasswordValidator<LoginData>, IPasswordHasher<LoginData>
{
	private readonly SplamyContext context;

	public UserStore(SplamyContext context)
	{
		this.context = context;
	}

	public async Task<LoginData?> GetUserByToken(string token)
	{
		if (token == null)
			return null;
		return await (from user in context.User.AsNoTracking()
					  where user.Token == token
					  select user).FirstOrDefaultAsync();
	}

	public static async Task InitializeAccountWhenEmpty(SplamyContext db, NLog.Logger logger)
	{
		if (!await db.User.AsNoTracking().AnyAsync())
		{
			logger.Info("Creating admin 'Splamy' acount");
			var rndPw = RandomToken(16);
			var rndToken = RandomToken();
			var (pw, salt) = HashPw(rndPw);
			await db.User.AddAsync(new LoginData("Splamy", pw, salt, rndToken, UserType.Admin));
			File.WriteAllText(Path.Combine(Util.DataPath, "token.tmp"), $"PW:{rndPw}\nToken:{rndToken}");
			await db.SaveChangesAsync();
		}
	}

	public static string RandomToken(int length = 64)
	{
		const string tokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		Span<byte> buffer = stackalloc byte[length];
		RandomNumberGenerator.Fill(buffer);
		var strb = new StringBuilder(buffer.Length);
		for (int i = 0; i < buffer.Length; i++)
			strb.Append(tokenChars[(tokenChars.Length - 1) * buffer[i] / 255]);
		return strb.ToString();
	}

	public static (byte[] password, byte[] salt) HashPw(string password)
	{
		// generate a 128-bit salt using a secure PRNG
		var salt = new byte[128 / 8];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(salt);
		return (HashPw(password, salt), salt);
	}

	public static byte[] HashPw(string password, byte[] salt)
	{
		// derive a 256-bit subkey
		return KeyDerivation.Pbkdf2(
			password: password,
			salt: salt,
			prf: KeyDerivationPrf.HMACSHA256,
			iterationCount: 10000,
			numBytesRequested: 256 / 8);
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
		await context.SaveChangesAsync(cancellationToken);
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
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task<IdentityResult> CreateAsync(LoginData role, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		try
		{
			await context.User.AddAsync(role, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
			return IdentityResult.Success;
		}
		catch { return IdentityResult.Failed(new IdentityError { Code = "UserAlreadyExists", Description = "Could not create because user already exists" }); }
	}

	// TODO ??? find out what this method actually does
	public async Task<IdentityResult> UpdateAsync(LoginData role, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		context.User.Update(role);
		await context.SaveChangesAsync(cancellationToken);
		return IdentityResult.Success;
		//: IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "The user to update could not be found" });
	}

	public async Task<IdentityResult> DeleteAsync(LoginData role, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		context.Remove(role.Id);
		await context.SaveChangesAsync(cancellationToken);
		return IdentityResult.Success;
		//: IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "The user to delete could not be found" });
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
		await context.SaveChangesAsync(cancellationToken);
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
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task<LoginData> FindByIdAsync(string roleId, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var irole = int.Parse(roleId, CultureInfo.InvariantCulture);
		// Using ! to ignor nullability warning since the interface for some reason doesn't declare it nullable
		return (await (from user in context.User.AsNoTracking()
					  where user.Id == irole
					  select user).FirstOrDefaultAsync(cancellationToken))!;
	}

	// TODO split up role <-> user
	public async Task<LoginData> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		// Using ! to ignor nullability warning since the interface for some reason doesn't declare it nullable
		return (await (from user in context.User.AsNoTracking()
					  where user.NameNormalized == normalizedRoleName
					  select user).FirstOrDefaultAsync(cancellationToken))!;
	}

	public async Task SetPasswordHashAsync(LoginData user, string passwordHash, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		user.Password = Convert.FromBase64String(passwordHash);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task<string> GetPasswordHashAsync(LoginData user, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Convert.ToBase64String(user.Password);
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
		return Convert.ToBase64String(HashPw(password, user.Salt));
	}

	public PasswordVerificationResult VerifyHashedPassword(LoginData user, string hashedPassword, string providedPassword)
	{
		if (HashPw(providedPassword, user.Salt).SequenceEqual(Convert.FromBase64String(hashedPassword)))
		{
			return PasswordVerificationResult.Success;
		}
		return PasswordVerificationResult.Failed;
	}

	public void Dispose()
	{
	}
#pragma warning restore CS1998

	#endregion
}
