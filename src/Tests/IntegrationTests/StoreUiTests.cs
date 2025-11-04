using System;
using System.Net;
using System.Net.Http.Json;
using DataEntities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Data;
using Store.Services;
using Xunit;

namespace IntegrationTests;

public class StoreUiTests
{
    [Fact]
    public async Task ProductsPage_LoadsAndContainsProductNames()
    {
        // Start the Products app in-memory
        await using var productsFactory = new WebApplicationFactory<Products.Program>();
   
        // Seed the Products database with test data
        using (var scope = productsFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDataContext>();
            db.Database.EnsureCreated();
            
            if (!db.Product.Any())
            {
                db.Product.Add(new Product { Name = "Solar Powered Flashlight", Description = "Test product", Price = 19.99m, ImageUrl = "product1.png" });
                db.SaveChanges();
            }
        }

        var productsClient = productsFactory.CreateClient();
        var productsBase = productsClient.BaseAddress?.ToString() ?? throw new InvalidOperationException("Products base address not available");

        // Use the Products test server base (do NOT append '/api/Product' here).
        // ProductService calls '/api/Product' relative to its BaseAddress, so providing the server root avoids duplicated paths.
        var productApiBase = productsBase.TrimEnd('/');

        // Start the Store app but override configuration so it calls the in-memory Products server
        // ALSO override the Store's HttpClient registrations to route outgoing HTTP calls
        // into the in-memory Products server (avoids connecting to localhost:80).
        await using var storeFactory = new WebApplicationFactory<Store.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, conf) =>
                {
                    // Inject ProductEndpoint that points to the running Products test server root
                    var dict = new Dictionary<string, string?>
                    {
                        ["ProductEndpoint"] = productApiBase
                    };
                    conf.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    // Override the typed HttpClient for ProductService to use the in-memory Products TestServer.
                    services.AddHttpClient<ProductService>(client =>
                    {
                        client.BaseAddress = new Uri(productApiBase);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => productsFactory.Server.CreateHandler());
                });
            });

        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var response = await client.GetAsync("/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Write HTML to test output for debugging
        Console.WriteLine("===== STORE PAGE HTML START =====");
        Console.WriteLine(html);
        Console.WriteLine("===== STORE PAGE HTML END =====");

        html.Should().Contain("Products");
        // check at least one seeded product name exists
        html.Should().Contain("Solar Powered Flashlight");
    }

    [Fact]
    public async Task HomePage_LoadsSuccessfully()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Welcome to TinyShop!");
        html.Should().Contain("The best e-commerce platform for outdoor enthusiasts");
    }

    [Fact]
    public async Task HomePage_DisplaysFeaturedProducts()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts(count: 5);
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Featured Products");
        html.Should().Contain("Test Product 1");
        html.Should().Contain("Test Product 2");
        html.Should().Contain("Test Product 3");
    }

    [Fact]
    public async Task HomePage_ShowsShopNowButton()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Shop Now");
        html.Should().Contain("btn btn-primary btn-lg");
    }

    [Fact]
    public async Task HomePage_ShowsViewAllProductsButton()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("View All Products");
    }

    [Fact]
    public async Task HomePage_DisplaysNoProductsMessage_WhenCatalogEmpty()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts(count: 0);
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("No products available yet");
    }

    [Fact]
    public async Task ProductsPage_DisplaysProductCount()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts(count: 5);
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().MatchRegex(@"Showing \d+ products?");
    }

    [Fact]
    public async Task ProductsPage_ShowsDebugViewButton()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Debug View");
        html.Should().Contain("/products/debug");
    }

    [Fact]
    public async Task ProductsPage_DisplaysAddToCartButtons()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts(count: 3);
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Add to Cart");
        html.Should().Contain("bi-cart-plus");
    }

    [Fact]
    public async Task ProductsPage_DisplaysProductPrices()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // Check for currency formatting
        html.Should().MatchRegex(@"\$\d+\.\d{2}|\€\d+,\d{2}");
    }

    [Fact]
    public async Task ProductsPage_ShowsEmptyMessage_WhenNoProducts()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts(count: 0);
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("No Products Available");
        html.Should().Contain("The product catalog is currently empty");
    }

    [Fact]
    public async Task AboutPage_LoadsSuccessfully()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/about");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("About TinyShop");
    }

    [Fact]
    public async Task AboutPage_ShowsWarning_WhenNoQueryParams()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/about");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("This page requires query parameters");
        html.Should().Contain("?param=value");
    }

    [Fact]
    public async Task AboutPage_ShowsWelcome_WhenQueryParamsPresent()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/about?test=value");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Welcome to the About page");
        html.Should().Contain("Query parameters detected");
    }

    [Fact]
    public async Task Navigation_HomeLink_IsPresent()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Home");
        html.Should().Contain("bi-house-door-fill");
    }

    [Fact]
    public async Task Navigation_ProductsLink_IsPresent()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Products");
        html.Should().Contain("bi-list-nested");
    }

    [Fact]
    public async Task Navigation_BrandLink_IsPresent()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("TinyShop");
        html.Should().Contain("navbar-brand");
    }

    [Fact]
    public async Task ProductsPage_DisplaysLoadingSpinner_Initially()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // The HTML should contain spinner classes (even if not visible in final render)
        html.Should().Contain("spinner-border");
    }

    [Fact]
    public async Task HomePage_ContainsPageTitle()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("TinyShop - Home");
    }

    [Fact]
    public async Task ProductsPage_ContainsPageTitle()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Products - TinyShop");
    }

    [Fact]
    public async Task ProductsPage_DisplaysProductImages()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("product-image");
        html.Should().Contain("loading=\"lazy\"");
        html.Should().Contain("onerror");
    }

    [Fact]
    public async Task HomePage_DisplaysViewDetailsButtons()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts(count: 3);
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("View Details");
    }

    [Fact]
    public async Task ProductsPage_ShowsProductDescriptions()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Description for Test Product");
    }

    [Fact]
    public async Task ProductsPage_UsesResponsiveGrid()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("products-grid");
        html.Should().Contain("product-card");
    }

    [Fact]
    public async Task HomePage_UsesBootstrapClasses()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("container");
        html.Should().Contain("row");
        html.Should().Contain("col-md-4");
        html.Should().Contain("card");
    }

    [Fact]
    public async Task AllPages_ReturnOkStatus()
    {
        var (storeFactory, _) = await CreateTestFactoriesWithProducts();
        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var pages = new[] { "/", "/products", "/about" };

        foreach (var page in pages)
        {
            var response = await client.GetAsync(page);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"because {page} should load successfully");
        }
    }

    // Helper method to create test factories with seeded products
    private async Task<(WebApplicationFactory<Store.Program> storeFactory, WebApplicationFactory<Products.Program> productsFactory)> 
        CreateTestFactoriesWithProducts(int count = 3)
    {
        var productsFactory = new WebApplicationFactory<Products.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
            });

        // Seed the Products database with test data
        using (var scope = productsFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDataContext>();
            db.Database.EnsureCreated();

            if (db.Product.Any())
            {
                db.Product.RemoveRange(db.Product);
                await db.SaveChangesAsync();
            }

            for (int i = 1; i <= count; i++)
            {
                db.Product.Add(new Product
                {
                    Name = $"Test Product {i}",
                    Description = $"Description for Test Product {i}",
                    Price = 10.00m * i,
                    ImageUrl = $"product{i}.png",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        var productsClient = productsFactory.CreateClient();
        var productsBase = productsClient.BaseAddress?.ToString()
            ?? throw new InvalidOperationException("Products base address not available");
        var productApiBase = productsBase.TrimEnd('/');

        var storeFactory = new WebApplicationFactory<Store.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((context, conf) =>
                {
                    var dict = new Dictionary<string, string?>
                    {
                        ["ProductEndpoint"] = productApiBase
                    };
                    conf.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    services.AddHttpClient<ProductService>(client =>
                    {
                        client.BaseAddress = new Uri(productApiBase);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => productsFactory.Server.CreateHandler());
                });
            });

        return (storeFactory, productsFactory);
    }
}
