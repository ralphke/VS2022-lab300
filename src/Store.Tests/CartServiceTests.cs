using DataEntities;
using FluentAssertions;
using Store.Services;
using Xunit;

namespace Store.Tests;

public class CartServiceTests
{
    [Fact]
    public void AddItem_WhenProductIsNew_ShouldAddToCart()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };

        // Act
        cartService.AddItem(product);

        // Assert
        cartService.GetItemCount().Should().Be(1);
        cartService.GetItems().Should().HaveCount(1);
        cartService.GetItems().First().Product.Should().Be(product);
        cartService.GetItems().First().Quantity.Should().Be(1);
    }

    [Fact]
    public void AddItem_WhenProductExists_ShouldIncreaseQuantity()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };

        // Act
        cartService.AddItem(product);
        cartService.AddItem(product);

        // Assert
        cartService.GetItemCount().Should().Be(2);
        cartService.GetItems().Should().HaveCount(1);
        cartService.GetItems().First().Quantity.Should().Be(2);
    }

    [Fact]
    public void RemoveItem_WhenQuantityIsOne_ShouldRemoveFromCart()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
        cartService.AddItem(product);

        // Act
        cartService.RemoveItem(1);

        // Assert
        cartService.GetItemCount().Should().Be(0);
        cartService.GetItems().Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_WhenQuantityIsGreaterThanOne_ShouldDecreaseQuantity()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
        cartService.AddItem(product);
        cartService.AddItem(product);

        // Act
        cartService.RemoveItem(1);

        // Assert
        cartService.GetItemCount().Should().Be(1);
        cartService.GetItems().Should().HaveCount(1);
        cartService.GetItems().First().Quantity.Should().Be(1);
    }

    [Fact]
    public void RemoveAll_ShouldRemoveAllQuantitiesOfProduct()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
        cartService.AddItem(product);
        cartService.AddItem(product);
        cartService.AddItem(product);

        // Act
        cartService.RemoveAll(1);

        // Assert
        cartService.GetItemCount().Should().Be(0);
        cartService.GetItems().Should().BeEmpty();
    }

    [Fact]
    public void GetTotal_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var cartService = new CartService();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20.00m };

        // Act
        cartService.AddItem(product1);
        cartService.AddItem(product1);
        cartService.AddItem(product2);

        // Assert
        cartService.GetTotal().Should().Be(40.00m); // 10*2 + 20*1
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var cartService = new CartService();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20.00m };
        cartService.AddItem(product1);
        cartService.AddItem(product2);

        // Act
        cartService.Clear();

        // Assert
        cartService.GetItemCount().Should().Be(0);
        cartService.GetItems().Should().BeEmpty();
        cartService.GetTotal().Should().Be(0);
    }

    [Fact]
    public void OnChange_ShouldBeTriggeredWhenItemAdded()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
        var eventTriggered = false;
        cartService.OnChange += () => eventTriggered = true;

        // Act
        cartService.AddItem(product);

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void OnChange_ShouldBeTriggeredWhenItemRemoved()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
        cartService.AddItem(product);
        var eventTriggered = false;
        cartService.OnChange += () => eventTriggered = true;

        // Act
        cartService.RemoveItem(1);

        // Assert
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void OnChange_ShouldBeTriggeredWhenCartCleared()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };
        cartService.AddItem(product);
        var eventTriggered = false;
        cartService.OnChange += () => eventTriggered = true;

        // Act
        cartService.Clear();

        // Assert
        eventTriggered.Should().BeTrue();
    }
}
