using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;

namespace Products.Services;

public sealed class ProductSearchService
{
    public record SearchResult(List<Product> Products, int Total, int Page, int PageSize, int TotalPages);

    private readonly ProductDataContext _context;
    private readonly IEmbeddingService _embeddings;

    public ProductSearchService(ProductDataContext context, IEmbeddingService embeddings)
    {
        _context = context;
        _embeddings = embeddings;
    }

    /// <summary>
    /// Search products by keyword with pagination. Returns at most 5 results per page.
    /// </summary>
    public async Task<SearchResult> SearchByKeywordAsync(string? keyword, int page = 1, int pageSize = 5)
    {
        var pageSize_ = Math.Min(pageSize, 5); // Cap at 5
        var skip = (page - 1) * pageSize_;

        var query = _context.Product.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(kw) ||
                p.Description.ToLower().Contains(kw) ||
                p.Details.ToLower().Contains(kw));
        }

        var total = await query.CountAsync();
        var products = await query
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(pageSize_)
            .ToListAsync();

        var totalPages = (total + pageSize_ - 1) / pageSize_;
        return new SearchResult(products, total, page, pageSize_, totalPages);
    }

    /// <summary>
    /// Semantic search using vector similarity (if embeddings are populated).
    /// Falls back to keyword search if no embeddings available.
    /// Returns at most 5 results per page.
    /// </summary>
    public async Task<SearchResult> SearchBySemanticAsync(string query, int page = 1, int pageSize = 5)
    {
        var pageSize_ = Math.Min(pageSize, 5);
        var skip = (page - 1) * pageSize_;

        // Generate embedding for the query
        var queryEmbedding = await _embeddings.EmbedTextAsync(query);

        // Check if any products have embeddings
        var hasAnyEmbeddings = await _context.Product
            .AnyAsync(p => p.DescriptionEmbedding != null);

        if (!hasAnyEmbeddings)
        {
            // Fallback to keyword search
            return await SearchByKeywordAsync(query, page, pageSize_);
        }

        // Vector similarity search: find products with highest cosine similarity
        // NOTE: SQL Server 2024+ supports vector search natively with vector_distance function
        // For earlier versions, fetch all products and score in memory
        var products = await _context.Product
            .Where(p => p.DescriptionEmbedding != null)
            .ToListAsync();

        var scored = products
            .Select(p => new { Product = p, Score = CosineSimilarity(queryEmbedding, p.DescriptionEmbedding!) })
            .OrderByDescending(x => x.Score)
            .Skip(skip)
            .Take(pageSize_)
            .Select(x => x.Product)
            .ToList();

        var total = products.Count;
        var totalPages = (total + pageSize_ - 1) / pageSize_;
        return new SearchResult(scored, total, page, pageSize_, totalPages);
    }

    /// <summary>
    /// Get all products with pagination.
    /// </summary>
    public async Task<SearchResult> ListAllAsync(int page = 1, int pageSize = 5)
    {
        var pageSize_ = Math.Min(pageSize, 5);
        var skip = (page - 1) * pageSize_;

        var total = await _context.Product.CountAsync();
        var products = await _context.Product
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Take(pageSize_)
            .ToListAsync();

        var totalPages = (total + pageSize_ - 1) / pageSize_;
        return new SearchResult(products, total, page, pageSize_, totalPages);
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors.
    /// </summary>
    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0;

        var dotProduct = 0f;
        var normA = 0f;
        var normB = 0f;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denominator == 0 ? 0 : dotProduct / denominator;
    }
}
