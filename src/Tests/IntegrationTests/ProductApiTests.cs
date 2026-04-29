using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using DataEntities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Products.Data;
using Products.Services;
using Xunit;

namespace IntegrationTests;

public class ProductApiTests : IClassFixture<ProductApiTests.ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;

    public ProductApiTests(ProductsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllProducts_ReturnsSuccessAndList()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Product/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeNull();
        products.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetProductById_ReturnsProduct_WhenExists()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Product/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
    }

    [Fact]
    public async Task LoadImagesAsync_PopulatesImageData_ForExistingProduct_WithImageUrl()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "wwwroot", "images"));
        var imagePath = Path.Combine(tempRoot, "wwwroot", "images", "product1.png");
        await File.WriteAllBytesAsync(imagePath, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var originalCurrentDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempRoot);

            var options = new DbContextOptionsBuilder<ProductDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

            await using var db = new ProductDataContext(options);
            db.Database.EnsureCreated();

            db.Product.Add(new Product
            {
                Name = "Test Product",
                Description = "A test product",
                Price = 1.00m,
                ImageUrl = "images/product1.png",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var logger = NullLogger<ProductDataContext>.Instance;
            var dbInitializerType = typeof(ProductDataContext).Assembly.GetType("Products.Data.DbInitializer", throwOnError: true)!;
            var method = dbInitializerType.GetMethod("LoadImagesAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
            await (Task)method.Invoke(null, new object[] { db, logger })!;

            var savedProduct = await db.Product.SingleAsync();
            savedProduct.ImageData.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    public sealed class ProductsApiFactory : WebApplicationFactory<Products.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var connectionString = ResolveTinyShopDbConnectionString();

                services.RemoveAll<DbContextOptions<ProductDataContext>>();
                services.RemoveAll<ProductDataContext>();
                services.AddDbContext<ProductDataContext>(options =>
                {
                    options.UseSqlServer(connectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null));
                });

                services.RemoveAll<IEmbeddingService>();
                services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
            });
        }

        private static string ResolveTinyShopDbConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TinyShopDB")
                ?? Environment.GetEnvironmentVariable("PRODUCTS_DB_CONNECTION_STRING");

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            var saPassword = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
            if (string.IsNullOrWhiteSpace(saPassword))
            {
                throw new InvalidOperationException("Set ConnectionStrings__TinyShopDB, PRODUCTS_DB_CONNECTION_STRING, or MSSQL_SA_PASSWORD.");
            }

            return $"Server=localhost,1433;Database=TinyShopDB;User Id=sa;Password={saPassword};TrustServerCertificate=True;Encrypt=False;";
        }
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        private static readonly float[] Vector = new float[768];

        public Task<float[]> EmbedTextAsync(string text)
        {
            return Task.FromResult(Vector);
        }

        public Task<IDictionary<string, float[]>> EmbedBatchAsync(IEnumerable<string> texts)
        {
            var result = texts.ToDictionary(text => text, _ => Vector);
            return Task.FromResult<IDictionary<string, float[]>>(result);
        }
    }
}
