using DataEntities;
using Store.Services;

namespace Store.UnitTests;

public class CartServiceTests
{
    private Product CreateTestProduct(int id = 1, string name = "Test Product", decimal price = 10.00m)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = "Test Description",
            Price = price,
            ImageUrl = "test.jpg"
        };
    }

    [Fact]
    public void AddItem_NewProduct_AddsItemAndIncreasesCount()
    {
        // Arrange
        var cart = new CartService();
        var product = CreateTestProduct();

        // Act
        cart.AddItem(product, 2);

        // Assert
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[product.Id].Quantity);
        Assert.Equal(product.Id, cart.Items[product.Id].Product.Id);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncrementsQuantity()
    {
        // Arrange
        var cart = new CartService();
        var product = CreateTestProduct();
        cart.AddItem(product, 1);

        // Act
        cart.AddItem(product, 2);

        // Assert
        Assert.Single(cart.Items);
        Assert.Equal(3, cart.Items[product.Id].Quantity);
    }

    [Fact]
    public void RemoveItem_DecrementsOrRemovesItem()
    {
        // Arrange
        var cart = new CartService();
        var product = CreateTestProduct();
        cart.AddItem(product, 3);

        // Act - decrement
        cart.RemoveItem(product.Id, 1);

        // Assert - still in cart with reduced quantity
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[product.Id].Quantity);

        // Act - remove all remaining
        cart.RemoveItem(product.Id, 2);

        // Assert - removed from cart
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void RemoveAll_RemovesAllQuantities()
    {
        // Arrange
        var cart = new CartService();
        var product1 = CreateTestProduct(1, "Product 1");
        var product2 = CreateTestProduct(2, "Product 2");
        cart.AddItem(product1, 5);
        cart.AddItem(product2, 3);

        // Act
        cart.RemoveAll(product1.Id);

        // Assert
        Assert.Single(cart.Items);
        Assert.False(cart.Items.ContainsKey(product1.Id));
        Assert.True(cart.Items.ContainsKey(product2.Id));
    }

    [Fact]
    public void GetTotal_ComputesCorrectTotal()
    {
        // Arrange
        var cart = new CartService();
        var product1 = CreateTestProduct(1, "Product 1", 10.00m);
        var product2 = CreateTestProduct(2, "Product 2", 15.50m);

        // Act
        cart.AddItem(product1, 2);  // 2 * 10.00 = 20.00
        cart.AddItem(product2, 3);  // 3 * 15.50 = 46.50
        var total = cart.GetTotal();

        // Assert
        Assert.Equal(66.50m, total);
    }

    [Fact]
    public void Clear_EmptiesCart()
    {
        // Arrange
        var cart = new CartService();
        var product1 = CreateTestProduct(1);
        var product2 = CreateTestProduct(2);
        cart.AddItem(product1, 2);
        cart.AddItem(product2, 1);

        // Act
        cart.Clear();

        // Assert
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.GetTotal());
    }

    [Fact]
    public void OnChange_IsRaisedOnMutations()
    {
        // Arrange
        var cart = new CartService();
        var product = CreateTestProduct();
        var changeCount = 0;
        cart.OnChange += () => changeCount++;

        // Act & Assert
        cart.AddItem(product, 1);
        Assert.Equal(1, changeCount);

        cart.AddItem(product, 1);
        Assert.Equal(2, changeCount);

        cart.RemoveItem(product.Id, 1);
        Assert.Equal(3, changeCount);

        cart.RemoveAll(product.Id);
        Assert.Equal(4, changeCount);

        cart.AddItem(product, 1);
        Assert.Equal(5, changeCount);

        cart.Clear();
        Assert.Equal(6, changeCount);
    }

    [Fact]
    public void AddItem_WithInvalidQuantity_ThrowsException()
    {
        // Arrange
        var cart = new CartService();
        var product = CreateTestProduct();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cart.AddItem(product, 0));
        Assert.Throws<ArgumentException>(() => cart.AddItem(product, -1));
    }

    [Fact]
    public void RemoveItem_WithInvalidQuantity_ThrowsException()
    {
        // Arrange
        var cart = new CartService();
        var product = CreateTestProduct();
        cart.AddItem(product, 5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cart.RemoveItem(product.Id, 0));
        Assert.Throws<ArgumentException>(() => cart.RemoveItem(product.Id, -1));
    }
}
