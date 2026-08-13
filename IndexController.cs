using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly ProductIndexer _indexer;
    public IndexController(ProductIndexer indexer) => _indexer = indexer;

    [HttpPost("ensure")]
    public async Task<IActionResult> Ensure()
    {
        await _indexer.EnsureCollectionAsync();
        return Ok("collection ensured");
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var products = new[]
        {
            new { Id = "p-1", Title = "گوشی مدل A", Description = "گوشی هوشمند با 6GB رم، 128GB حافظه، دوربین 48MP", Url = "https://shop.example/product/p-1" },
            new { Id = "p-2", Title = "هدفون بی‌سیم B", Description = "هدفون بلوتوثی با نویزکنسلینگ و عمر باتری 30 ساعت", Url = "https://shop.example/product/p-2" }
        };

        foreach (var p in products)
        {
            await _indexer.UpsertProductAsync(p.Id, p.Title, p.Description, p.Url);
        }

        return Ok("seeded");
    }
}
