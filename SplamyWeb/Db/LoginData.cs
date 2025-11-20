using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SplamyWeb.Db;
#pragma warning disable CS8618

[Table("user")]
public class LoginData
{
	[Key]
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? NameNormalized { get; set; }
	public byte[] Password { get; set; }
	public byte[] Salt { get; set; }
	public string Token { get; set; }
	public UserType Rank { get; set; }

	public LoginData() { }

	public LoginData(string? name, byte[] password, byte[] salt, string token, UserType rank)
	{
		Name = name;
		NameNormalized = NormalizeName(name);
		Password = password;
		Salt = salt;
		Token = token;
		Rank = rank;
	}

	public void SetName(string name)
	{
		Name = name;
		NameNormalized = NormalizeName(name);
	}

	public static string? NormalizeName(string? name) => name?.ToUpperInvariant();

	public bool CanEditOtherUser() => Rank.AtLeast(UserType.Admin);
	public bool CanSetRankUpTo(UserType targetRank) => targetRank <= Rank.CanSetRankUpTo();
}

#pragma warning restore CS8618

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
	public const string Admin = nameof(UserType.Admin);
	public const string User = nameof(UserType.User);

	extension(UserType self)
	{
		public bool AtLeast(UserType rankOrHigher)
		{
			return self <= rankOrHigher;
		}

		public UserType? CanSetRankUpTo() => self switch
		{
			UserType.Admin => UserType.Admin,
			_ => null,
		};
	}
}
