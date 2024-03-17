#addin nuget:?package=Cake.WinSCP&version=0.5.0

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does(() =>
{
    DotNetBuild("./SplamyWeb/SplamyWeb.csproj", new DotNetBuildSettings
    {
        Configuration = configuration,
    });
});

Task("Publish")
    .Does(() =>
{
    DotNetPublish("./SplamyWeb/SplamyWeb.csproj", new DotNetPublishSettings
    {
		Configuration = "Release",
		OutputDirectory = "./publish/"
    });
});

Task("Deploy")
	//.IsDependentOn("Publish")
	.Does(() =>
{
	WinScpSync(
		new WinSCP.SessionOptions() {
			Protocol = WinSCP.Protocol.Sftp,
			HostName = "splamy.de",
			PortNumber = 4242,
			UserName = "splamy",
			SshHostKeyFingerprint = "ecdsa-sha2-nistp256 AAAAE2VjZHNhLXNoYTItbmlzdHAyNTYAAAAIbmlzdHAyNTYAAABBBLQGiSWPGcNpt7unIT84mybx1xA/DQPL3ar5Ft1C/tXkafeC9CgZzIv+q+BKvfaV9J0wK6E4ifEwSqyaaB0Jq38=",
			SshPrivateKey = File.ReadAllText(@"C:\Users\Splamy\.ssh\id_ed25519")
		},
		"~/splstest/",
		@"./publish/",
		false,
		WinSCP.SynchronizationMode.Remote,
		false,
		WinSCP.SynchronizationCriteria.Time,
		new WinSCP.TransferOptions()
	);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetTest("./SplamyWeb/SplamyWeb.csproj", new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
    });
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
