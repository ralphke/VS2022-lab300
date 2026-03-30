using System.Net;
using System.Net.Http.Json;
using DataEntities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IntegrationTests;

public class ProductApiTests : IClassFixture<WebApplicationFactory<Products.Program>>
{
 private readonly WebApplicationFactory<Products.Program> _factory;

 public ProductApiTests(WebApplicationFactory<Products.Program> factory)
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
}
