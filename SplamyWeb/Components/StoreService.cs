using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SplamyWeb.Db;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class StoreService
{
	private (Dictionary<string, string?> dict, List<StoreEntry> list)? _cache;
	private readonly IServiceScopeFactory scopeFactory;

	public StoreService(IServiceScopeFactory scopeFactory)
	{
		this.scopeFactory = scopeFactory;
	}

	private async ValueTask<(Dictionary<string, string?> dict, List<StoreEntry> list)> GetAllInternal()
	{
		if (!_cache.HasValue)
		{
			using var scope = scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
			var list = await db.StoreTable.AsNoTracking().ToListAsync();
			var dict = list.ToDictionary(x => x.Id, x => x.Value);
			_cache = (dict, list);
		}
		return _cache.Value;
	}

	public async Task<IEnumerable<StoreEntry>> GetAll()
	{
		var (_, list) = await GetAllInternal();
		return list;
	}

	public async ValueTask<string?> Get(string key)
	{
		var (dict, _) = await GetAllInternal();
		return dict.TryGetValue(key, out var value) ? value : null;
	}

	public async Task Delete(string key)
	{
		_cache = null;
		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
		db.Remove(new StoreEntry(key, null));
		await db.SaveChangesAsync();
	}

	public async Task Set(string key, string? value)
	{
		_cache = null;
		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<SplamyContext>();
		await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO kvp_store (\"Id\",\"Value\") VALUES ({key}, {value}) ON CONFLICT(\"Id\") DO UPDATE SET \"Value\" = {value};");
	}

	private const string KeyTransifexAuth = "transifex_auth";
	public ValueTask<string?> GetTransifexAuth() => Get(KeyTransifexAuth);

	private const string KeyGithubAuth = "github_auth";
	public ValueTask<string?> GetGithubAuth() => Get(KeyGithubAuth);

	private const string KeyBlogMainTag = "blog_maintag";
	public ValueTask<string?> GetBlogMainTag() => Get(KeyBlogMainTag);
}
