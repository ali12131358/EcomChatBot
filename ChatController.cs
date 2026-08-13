using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ProductIndexer _indexer;
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public ChatController(ProductIndexer indexer, IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        _indexer = indexer;
        _http = httpFactory.CreateClient();
        _cfg = cfg;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req)
    {
        var embedding = await _indexer.GetEmbeddingAsync(req.Message);
        var searchRes = await _indexer.SearchAsync(embedding, top: 4);

        var sb = new StringBuilder();
        if (searchRes.RootElement.TryGetProperty("result", out var results))
        {
            foreach (var r in results.EnumerateArray())
            {
                if (r.TryGetProperty("payload", out var payload))
                {
                    var title = payload.GetProperty("title").GetString();
                    var url = payload.GetProperty("url").GetString();
                    var desc = payload.GetProperty("description").GetString();
                    sb.AppendLine($"- {title}: {desc} ({url})");
                }
            }
        }

        var prompt = $"شما دستیار فروش هستید. کاربر پرسیده: {req.Message}\n\nاسناد مرتبط:\n{sb}\n\nبه فارسی، کوتاه و دقیق پاسخ دهید و اگر نیاز شد لینک محصول را ذکر کنید. اگر اطلاعات کافی نیست، بگویید 'اطلاعات موجود نیست'.";

        var chatReq = new
        {
            model = _cfg["OpenAI:ChatModel"],
            messages = new[] {
                new { role = "system", content = "You are a helpful Persian e-commerce assistant." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        message.Headers.Add("Authorization", $"Bearer {_cfg["OpenAI:ApiKey"]}");
        message.Content = new StringContent(JsonSerializer.Serialize(chatReq), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(message);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return Ok(new { answer });
    }
}

public class ChatRequest
{
    public string Message { get; set; }
}
