using LiteDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SplamyWeb.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class StoreService
	{
		private readonly IServiceScopeFactory scopeFactory;

		public StoreService(IServiceScopeFactory scopeFactory)
		{
			this.scopeFactory = scopeFactory;
		}

		public IEnumerable<StoreEntry> GetAll() => DbQuery(storeTable => { return storeTable.ToList(); });
		public string? Get(string key) => DbQuery(storeTable => { return storeTable.FirstOrDefault(x => x.Id == key)?.Value; });
		public void Delete(string key) => DbAction(storeTable => { storeTable.Remove(new StoreEntry(key, null)); });
		public void Set(string key, string? value) => DbAction(storeTable => { storeTable.Upsert(new StoreEntry(key, value)).Run(); });

		private T DbQuery<T>(Func<DbSet<StoreEntry>, T> action)
		{
			using var scope = scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
			return action(db.StoreTable);
		}

		private void DbAction(Action<DbSet<StoreEntry>> action)
		{
			using var scope = scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
			action(db.StoreTable);
			db.SaveChanges();
		}

		private const string KeyTransifexAuth = "transifex_auth";
		public string? TransifexAuth { get => Get(KeyTransifexAuth); set => Set(KeyTransifexAuth, value); }

		private const string KeyGithubAuth = "github_auth";
		public string? GithubAuth { get => Get(KeyGithubAuth); set => Set(KeyGithubAuth, value); }
	}
}
