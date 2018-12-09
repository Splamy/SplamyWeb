using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HengoController : ControllerBase
    {
        private static readonly HttpClient web = new HttpClient();

        [HttpGet("update")]
        public async Task<IActionResult> UpdateAsync()
        {
            try
            {
                var site = await web.GetAsync("https://github.com/Hengoo/Fataman/archive/master.zip");
                using (var stream = await site.Content.ReadAsStreamAsync())
                using (var zip = new ZipArchive(stream))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        var target = Path.GetFullPath(Path.Combine(LocalDb.DataPath, "hengo", entry.FullName));
                        if (!target.StartsWith(LocalDb.DataPath))
                            continue;
                        if (entry.FullName.EndsWith('\\') || entry.FullName.EndsWith('/'))
                        {
                            var di = new DirectoryInfo(target);
                            if (!di.Exists)
                                di.Create();
                        }
                        else
                        {
                            var fileInfo = new FileInfo(target);
                            if (!fileInfo.Directory.Exists)
                                fileInfo.Directory.Create();
                            entry.ExtractToFile(target, true);
                        }
                    }
                }
                return Ok();
            }
            catch { return StatusCode(500); }
        }
    }
}