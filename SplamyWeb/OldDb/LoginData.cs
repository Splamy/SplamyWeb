using AutoMapper;
using SplamyWeb.Db;
using System;

namespace SplamyWeb.OldDb
{
	public class LoginData
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string NameNormalized { get; set; }
		public string Password { get; set; }
		public byte[] Salt { get; set; }
		public string Token { get; set; }
		public UserType Rank { get; set; }
	}

	public class TabStatsProfile : Profile
	{
		public TabStatsProfile()
		{
			CreateMap<LoginData, Db.LoginData>()
				.ConstructUsing(dst => new Db.LoginData())
				.ForMember(dst => dst.Password, x => x.MapFrom(src => Conv(src.Password)));
		}

		private static byte[] Conv(string s) => Convert.FromBase64String(s);
	}
#pragma warning restore CS8618
}
