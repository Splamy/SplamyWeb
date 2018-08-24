using System.Text.RegularExpressions;

namespace SplamyWeb
{
	public class Util
	{
		private static Regex saveRegex = new Regex(@"^\w+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

		public static bool IsSave(string param) => saveRegex.IsMatch(param);
	}
}
