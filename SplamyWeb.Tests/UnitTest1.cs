using Microsoft.VisualStudio.TestTools.UnitTesting;
using SplamyWeb.Db;

namespace SplamyWeb.Tests;

[TestClass]
public class UnitTest1
{
	[TestMethod]
	public void TestRanksAreCorrectlyOrdered()
	{
		Assert.IsTrue(UserType.Admin.AtLeast(UserType.Admin));
		Assert.IsTrue(UserType.Admin.AtLeast(UserType.User));
		Assert.IsTrue(UserType.User.AtLeast(UserType.User));
		Assert.IsFalse(UserType.User.AtLeast(UserType.Admin));
	}
}
