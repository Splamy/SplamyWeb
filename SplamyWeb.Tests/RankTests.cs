using SplamyWeb.Db;

namespace SplamyWeb.Tests;

public class RankTests
{
	[Fact]
	public void TestRanksAreCorrectlyOrdered()
	{
		Assert.True(UserType.Admin.AtLeast(UserType.Admin));
		Assert.True(UserType.Admin.AtLeast(UserType.User));
		Assert.True(UserType.User.AtLeast(UserType.User));
		Assert.False(UserType.User.AtLeast(UserType.Admin));
	}
}
