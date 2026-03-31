namespace Products.Services;

using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Interface for generating vector embeddings from text.
/// Embeddings are used for semantic search in the product catalog.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate a vector embedding from text.
    /// </summary>
    /// <param name="text">The text to embed (e.g., product description).</param>
    /// <returns>A vector embedding (float array, typically 1536 dimensions for Ada-3).</returns>
    Task<float[]> EmbedTextAsync(string text);

    /// <summary>
    /// Generate multiple embeddings in a batch.
    /// </summary>
    Task<IDictionary<string, float[]>> EmbedBatchAsync(IEnumerable<string> texts);
}

/// <summary>
/// Embedding service backed by a local HTTP embedding endpoint.
/// </summary>
public sealed class LocalEmbeddingService : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalEmbeddingService> _logger;
    private readonly string _embeddingEndpoint;

    public LocalEmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<LocalEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _embeddingEndpoint = configuration["EmbeddingService:Endpoint"] ?? "http://localhost:8001/embed";
    }

    public async Task<float[]> EmbedTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }

        var payloads = new object[]
        {
            new { inputs = text },
            new { inputs = new[] { text } }
        };

        Exception? lastError = null;

        foreach (var payload in payloads)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(_embeddingEndpoint, payload, JsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = new HttpRequestException($"Embedding endpoint returned {(int)response.StatusCode} ({response.ReasonPhrase}).");
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);
                var embedding = ExtractEmbedding(document.RootElement);
                if (embedding.Length == 0)
                {
                    lastError = new InvalidOperationException("Embedding endpoint returned an empty embedding vector.");
                    continue;
                }

                return embedding;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        _logger.LogError(lastError, "Failed to retrieve embedding from local endpoint {Endpoint}", _embeddingEndpoint);
        throw new InvalidOperationException($"Unable to generate embedding from endpoint '{_embeddingEndpoint}'.", lastError);
    }

    public async Task<IDictionary<string, float[]>> EmbedBatchAsync(IEnumerable<string> texts)
    {
        var result = new Dictionary<string, float[]>();
        foreach (var text in texts)
        {
            result[text] = await EmbedTextAsync(text);
        }
        return result;
    }

    private static float[] ExtractEmbedding(JsonElement root)
    {
        if (TryParseVector(root, out var vector))
        {
            return vector;
        }

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0 && TryParseVector(root[0], out vector))
        {
            return vector;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("embedding", out var embeddingNode) && TryParseVector(embeddingNode, out vector))
            {
                return vector;
            }

            if (root.TryGetProperty("vector", out var vectorNode) && TryParseVector(vectorNode, out vector))
            {
                return vector;
            }

            if (root.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Array && dataNode.GetArrayLength() > 0)
            {
                var first = dataNode[0];
                if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("embedding", out var nestedEmbedding) && TryParseVector(nestedEmbedding, out vector))
                {
                    return vector;
                }
            }

            if (root.TryGetProperty("embeddings", out var embeddingsNode))
            {
                if (TryParseVector(embeddingsNode, out vector))
                {
                    return vector;
                }

                if (embeddingsNode.ValueKind == JsonValueKind.Array && embeddingsNode.GetArrayLength() > 0 && TryParseVector(embeddingsNode[0], out vector))
                {
                    return vector;
                }
            }
        }

        return Array.Empty<float>();
    }

    private static bool TryParseVector(JsonElement element, out float[] vector)
    {
        vector = Array.Empty<float>();
        if (element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var values = new List<float>(element.GetArrayLength());
        foreach (var value in element.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            values.Add(value.GetSingle());
        }

        vector = values.ToArray();
        return vector.Length > 0;
    }
}
