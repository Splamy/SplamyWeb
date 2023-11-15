using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SplamyWeb.Components;
using System;
using System.Net;

namespace SplamyWeb.Tests;

[TestClass]
public class SpamCheckTests
{
	[TestMethod]
	public void SpamCheckerCorrectlyBlocksAfterSpam()
	{
		var spamChecker = new SpamBackingData(Substitute.For<ITimerService>(), TimeProvider.System);

		const int AllowedCallsBeforeBlock = 1000;
		var ip = new IPAddress(new byte[] { 192, 0, 0, 1 });

		for (int i = 0; i < AllowedCallsBeforeBlock; i++)
		{
			Assert.IsTrue(spamChecker.Check(ip), $"Check on call {i}");
		}

		Assert.IsFalse(spamChecker.Check(ip));
	}
}
