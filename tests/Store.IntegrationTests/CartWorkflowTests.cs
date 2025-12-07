using DataEntities;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Store.Services;

namespace Store.IntegrationTests;

public class CartWorkflowTests
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
    public void ScopedCart_IsolatedBetweenScopes()
    {
        // Arrange - Create a service collection similar to Store app
        var services = new ServiceCollection();
        services.AddScoped<CartService>();
        var serviceProvider = services.BuildServiceProvider();

        var product1 = CreateTestProduct(1, "Product 1");
        var product2 = CreateTestProduct(2, "Product 2");

        // Act - Create two separate scopes simulating two circuits
        using (var scope1 = serviceProvider.CreateScope())
        using (var scope2 = serviceProvider.CreateScope())
        {
            var cart1 = scope1.ServiceProvider.GetRequiredService<CartService>();
            var cart2 = scope2.ServiceProvider.GetRequiredService<CartService>();

            // Add different products to each cart
            cart1.AddItem(product1, 3);
            cart2.AddItem(product2, 5);

            // Assert - Carts are isolated
            Assert.Single(cart1.Items);
            Assert.Single(cart2.Items);
            Assert.True(cart1.Items.ContainsKey(product1.Id));
            Assert.False(cart1.Items.ContainsKey(product2.Id));
            Assert.True(cart2.Items.ContainsKey(product2.Id));
            Assert.False(cart2.Items.ContainsKey(product1.Id));
            Assert.Equal(3, cart1.Items[product1.Id].Quantity);
            Assert.Equal(5, cart2.Items[product2.Id].Quantity);
        }
    }

    [Fact]
    public void CartCleared_OnScopeDisposeViaCircuitHandler()
    {
        // Arrange - Create a service collection with CartService (scoped)
        var services = new ServiceCollection();
        services.AddScoped<CartService>();
        var serviceProvider = services.BuildServiceProvider();

        var product = CreateTestProduct(1, "Test Product");

        CartService? cart;
        bool changeEventFiredOnDispose = false;

        // Act - Create a scope, add items to cart, then dispose the scope
        using (var scope = serviceProvider.CreateScope())
        {
            cart = scope.ServiceProvider.GetRequiredService<CartService>();
            cart.AddItem(product, 2);

            // Subscribe to OnChange to verify Clear is called during Dispose
            cart.OnChange += () => changeEventFiredOnDispose = true;

            // Assert - Cart has items before disposal
            Assert.Single(cart.Items);
            Assert.Equal(2, cart.Items[product.Id].Quantity);
        }
        // Scope disposed here - cart should be disposed

        // Assert - OnChange event was fired when cart was cleared during disposal
        Assert.True(changeEventFiredOnDispose, "Cart should have fired OnChange when cleared during disposal");

        // Create a new scope to verify state is not leaked
        using (var newScope = serviceProvider.CreateScope())
        {
            var newCart = newScope.ServiceProvider.GetRequiredService<CartService>();

            // Assert - New cart is empty (no state leaked from previous scope)
            Assert.Empty(newCart.Items);
            Assert.Equal(0m, newCart.GetTotal());
        }
    }

    [Fact]
    public void MultipleScopesSequentially_NoStateLeak()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<CartService>();
        var serviceProvider = services.BuildServiceProvider();

        var product = CreateTestProduct(1, "Test Product", 15.00m);

        // Act & Assert - Create multiple scopes sequentially
        for (int i = 0; i < 3; i++)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var cart = scope.ServiceProvider.GetRequiredService<CartService>();

                // Each new scope should start with an empty cart
                Assert.Empty(cart.Items);

                // Add items
                cart.AddItem(product, i + 1);

                // Verify items were added
                Assert.Single(cart.Items);
                Assert.Equal(i + 1, cart.Items[product.Id].Quantity);
                Assert.Equal(15.00m * (i + 1), cart.GetTotal());
            }
            // Scope disposed - cart should be cleaned up
        }
    }

    [Fact]
    public void ServiceRegistrations_CanResolveCartServiceAndCircuitHandler()
    {
        // Arrange - Create a service collection matching Store app configuration
        var services = new ServiceCollection();
        services.AddScoped<CartService>();
        services.AddSingleton<CircuitHandler, CartCircuitHandler>();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Verify CartCircuitHandler can be resolved as singleton
        var circuitHandler1 = serviceProvider.GetServices<CircuitHandler>().OfType<CartCircuitHandler>().FirstOrDefault();
        var circuitHandler2 = serviceProvider.GetServices<CircuitHandler>().OfType<CartCircuitHandler>().FirstOrDefault();
        
        Assert.NotNull(circuitHandler1);
        Assert.NotNull(circuitHandler2);
        Assert.Same(circuitHandler1, circuitHandler2); // Should be same instance (singleton)

        // Verify CartService can be resolved as scoped
        using (var scope1 = serviceProvider.CreateScope())
        using (var scope2 = serviceProvider.CreateScope())
        {
            var cart1a = scope1.ServiceProvider.GetRequiredService<CartService>();
            var cart1b = scope1.ServiceProvider.GetRequiredService<CartService>();
            var cart2 = scope2.ServiceProvider.GetRequiredService<CartService>();

            Assert.NotNull(cart1a);
            Assert.NotNull(cart2);
            Assert.Same(cart1a, cart1b); // Same scope should return same instance
            Assert.NotSame(cart1a, cart2); // Different scopes should return different instances
        }
    }
}
