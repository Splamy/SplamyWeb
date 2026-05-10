using NSubstitute;
using SplamyWeb.Components;
using System;
using System.Net;

namespace SplamyWeb.Tests;

public class SpamCheckTests
{
	[Fact]
	public void SpamCheckerCorrectlyBlocksAfterSpam()
	{
		var spamChecker = new SpamBackingData(Substitute.For<ITimerService>(), TimeProvider.System);

		const int AllowedCallsBeforeBlock = 1000;
		var ip = new IPAddress([192, 0, 0, 1]);

		for (int i = 0; i < AllowedCallsBeforeBlock; i++)
		{
			Assert.True(spamChecker.Check(ip), $"Check on call {i}");
		}

		Assert.False(spamChecker.Check(ip));
	}
}
