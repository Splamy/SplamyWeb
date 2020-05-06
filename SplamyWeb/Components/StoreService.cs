using LiteDB;
using System.Collections.Generic;

namespace SplamyWeb.Components
{
	public class StoreService
	{
		private readonly ILiteCollection<StoreEntry> storeTable;

		public StoreService(LocalDb db)
		{
			storeTable = db.StoreTable;
		}

		public IEnumerable<StoreEntry> GetAll() => storeTable.FindAll();
		public string? Get(string key) => storeTable.FindById(key)?.Value;
		public void Delete(string key) => storeTable.Delete(key);
		public void Set(string key, string? value) => storeTable.Upsert(new StoreEntry() { Id = key, Value = value });

		private const string KeyTransifexAuth = "transifex_auth";
		public string? TransifexAuth { get => Get(KeyTransifexAuth); set => Set(KeyTransifexAuth, value); }

		private const string KeyGithubAuth = "github_auth";
		public string? GithubAuth { get => Get(KeyGithubAuth); set => Set(KeyGithubAuth, value); }
	}
}
