using System;
using System.Collections.Generic;
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

		public static TimeSpan Sum(this IEnumerable<TimeSpan?> source)
		{
			TimeSpan sum = TimeSpan.Zero;
			foreach (var v in source)
				if (v != null)
					sum += v.GetValueOrDefault();
			return sum;
		}
		public static uint Sum(this IEnumerable<uint> source)
		{
			uint sum = 0;
			foreach (var v in source)
				sum += v;
			return sum;
		}
		public static uint Sum(this IEnumerable<uint?> source)
		{
			uint sum = 0;
			foreach (var v in source)
				if (v != null)
					sum += v.GetValueOrDefault();
			return sum;
		}
	}
}
