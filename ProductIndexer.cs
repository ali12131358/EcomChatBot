using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class ProductIndexer
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly string _openAiKey;
    private readonly string _qdrantUrl;
    private readonly string _collection;

    public ProductIndexer(IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        _http = httpFactory.CreateClient();
        _cfg = cfg;
        _openAiKey = _cfg["OpenAI:ApiKey"];
        _qdrantUrl = _cfg["Qdrant:Url"] ?? "http://localhost:6333";
        _collection = _cfg["Qdrant:CollectionName"] ?? "products";
    }

    public async Task EnsureCollectionAsync()
    {
        var dimension = int.Parse(_cfg["Qdrant:EmbeddingDimension"] ?? "1536");
        var url = $"{_qdrantUrl}/collections/{_collection}";
        var body = JsonSerializer.Serialize(new { vectors = new { size = dimension, distance = "Cosine" } });
        var res = await _http.PutAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
        // Accept 200/201/409 (already exists) — in production check more carefully
        if (!res.IsSuccessStatusCode && res.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            res.EnsureSuccessStatusCode();
        }
    }

    public async Task<double[]> GetEmbeddingAsync(string text)
    {
        var req = new
        {
            model = _cfg["OpenAI:EmbeddingModel"],
            input = text
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        message.Headers.Add("Authorization", $"Bearer {_openAiKey}");
        message.Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
        var res = await _http.SendAsync(message);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray();
        return arr.Select(x => x.GetDouble()).ToArray();
    }

    public async Task UpsertProductAsync(string id, string title, string description, string url)
    {
        var docText = $"{title}\n\n{description}";
        var embedding = await GetEmbeddingAsync(docText);
        var point = new
        {
            id = id,
            vector = embedding,
            payload = new { title, url, description }
        };
        var upsert = new { points = new[] { point } };
        var putUrl = $"{_qdrantUrl}/collections/{_collection}/points?wait=true";
        var res = await _http.PostAsync(putUrl, new StringContent(JsonSerializer.Serialize(upsert), Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();
    }

    public async Task<JsonDocument> SearchAsync(double[] vector, int top = 4)
    {
        var url = $"{_qdrantUrl}/collections/{_collection}/points/search";
        var body = JsonSerializer.Serialize(new { vector = vector, limit = top });
        var res = await _http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();
        var s = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(s);
    }
}
