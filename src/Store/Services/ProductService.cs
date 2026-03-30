using DataEntities;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Store.Services;

public class ProductService
{
    private readonly HttpClient httpClient;
    private readonly string browserEndpoint;
    private readonly IMemoryCache cache;

    public ProductService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        this.httpClient = httpClient;
        // Get browser-accessible endpoint for image URLs (fallback to localhost:7130)
        this.browserEndpoint = configuration["ProductBrowserEndpoint"] ?? "https://localhost:7130";
   
        // Ensure trailing slash
        if (!this.browserEndpoint.EndsWith("/"))
        {
            this.browserEndpoint += "/";
        }
        this.cache = cache;
    }

    public async Task<List<Product>> GetProducts()
    {
        return await cache.GetOrCreateAsync("products", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5.0);
            List<Product>? products = null;
            var response = await httpClient.GetAsync("/api/Product");
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                products = await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.ListProduct);
            }

            return products ?? new List<Product>();
        });
    }

    /// <summary>
    /// Gets a limited number of products for page display
    /// </summary>
    /// <param name="pageSize">Number of products to retrieve (default: 12)</param>
    /// <returns>List of products limited to the specified page size</returns>
    public async Task<List<Product>> GetProductsPageAsync(int pageSize = 12)
    {
        var response = await httpClient.GetAsync($"/api/Product?size={pageSize}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.ListProduct) ?? new List<Product>();
        }
        return new List<Product>();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<Product>($"/api/Product/{id}");
    }

    public async Task<Product?> CreateProductAsync(Product product)
    {
        var response = await httpClient.PostAsJsonAsync("/api/Product", product);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task UpdateProductAsync(int id, Product product)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/Product/{id}", product);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProductAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"/api/Product/{id}");
        response.EnsureSuccessStatusCode();
    }

    public string GetImageUrl(Product product)
    {
        // Scenario 2: Database serving via /api/Product/{id}/image endpoint
        
        // Use browserEndpoint for image URLs (browser-accessible)
        // This is separate from httpClient.BaseAddress which may use service discovery
        
        // Priority 1: Use database image endpoint if product has an ID
        if (product.Id > 0)
        {
            return $"{browserEndpoint}api/Product/{product.Id}/image";
        }
        
        // Priority 2: Use ImageUrl if available (fallback to static files)
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            var imageUrl = product.ImageUrl.TrimStart('/');

            if (!imageUrl.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                imageUrl = $"images/{imageUrl}";
            }
   
            return $"{browserEndpoint}{imageUrl}";
        }
   
        // Priority 3: Return placeholder from Products static files
        return $"{browserEndpoint}images/placeholder.png";
    }
}