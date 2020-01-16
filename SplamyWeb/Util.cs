using System.Text.RegularExpressions;

namespace SplamyWeb
{
	public static class Util
	{
		public const string AuthScheme = "BasicAuthentication,Identity.Application";

		public static NLog.Targets.MemoryTarget NLogMemory = new NLog.Targets.MemoryTarget()
		{
			Layout = "${longdate} | ${level} | ${message}",
		};

		private static readonly Regex saveRegex = new Regex(@"^[\w-_]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

		public static bool IsSave(string param) => saveRegex.IsMatch(param);

		public static string Truncate(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}
	}
}
