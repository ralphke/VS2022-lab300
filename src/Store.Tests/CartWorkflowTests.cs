using Bunit;
using DataEntities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Store.Components.Pages;
using Store.Services;
using Xunit;

namespace Store.Tests;

public class CartWorkflowTests : TestContext
{
    [Fact]
    public void AddToCart_ShouldUpdateCartCounter()
    {
        // Arrange
        var cartService = new CartService();
        Services.AddScoped<CartService>(_ => cartService);
        
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };

        // Act
        cartService.AddItem(product);

        // Assert
        cartService.GetItemCount().Should().Be(1);
        cartService.GetItems().Should().ContainSingle()
            .Which.Product.Name.Should().Be("Test Product");
    }

    [Fact]
    public void MultipleProducts_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var cartService = new CartService();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 15.99m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 25.50m };
        var product3 = new Product { Id = 3, Name = "Product 3", Price = 9.99m };

        // Act
        cartService.AddItem(product1);
        cartService.AddItem(product1); // Add twice
        cartService.AddItem(product2);
        cartService.AddItem(product3);
        cartService.AddItem(product3);
        cartService.AddItem(product3); // Add three times

        // Assert
        cartService.GetItemCount().Should().Be(6); // 2+1+3
        cartService.GetTotal().Should().Be(87.45m); // (15.99*2) + (25.50*1) + (9.99*3)
    }

    [Fact]
    public void RemoveProduct_ShouldUpdateTotals()
    {
        // Arrange
        var cartService = new CartService();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20.00m };
        
        cartService.AddItem(product1);
        cartService.AddItem(product1);
        cartService.AddItem(product2);

        // Act
        cartService.RemoveItem(1); // Remove one of product1

        // Assert
        cartService.GetItemCount().Should().Be(2); // 1+1
        cartService.GetTotal().Should().Be(30.00m); // (10*1) + (20*1)
    }

    [Fact]
    public void Checkout_ShouldClearCart()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        cartService.AddItem(product);
        cartService.AddItem(product);

        // Act - Simulate checkout
        cartService.Clear();

        // Assert
        cartService.GetItemCount().Should().Be(0);
        cartService.GetTotal().Should().Be(0);
        cartService.GetItems().Should().BeEmpty();
    }

    [Fact]
    public void CartService_ShouldHandleEdgeCases()
    {
        // Arrange
        var cartService = new CartService();

        // Act & Assert - Remove from empty cart
        cartService.RemoveItem(999);
        cartService.GetItemCount().Should().Be(0);

        // Act & Assert - Remove non-existent product
        var product = new Product { Id = 1, Name = "Product", Price = 10.00m };
        cartService.AddItem(product);
        cartService.RemoveItem(999);
        cartService.GetItemCount().Should().Be(1);
    }

    [Fact]
    public void OnChange_Event_ShouldTriggerUIUpdate()
    {
        // Arrange
        var cartService = new CartService();
        var changeCount = 0;
        cartService.OnChange += () => changeCount++;
        
        var product = new Product { Id = 1, Name = "Product", Price = 10.00m };

        // Act
        cartService.AddItem(product);
        cartService.AddItem(product);
        cartService.RemoveItem(1);
        cartService.Clear();

        // Assert
        changeCount.Should().Be(4); // Add, Add, Remove, Clear
    }
}
