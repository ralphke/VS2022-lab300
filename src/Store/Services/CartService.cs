using DataEntities;

namespace Store.Services;

public class CartItem
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public class CartService
{
    private readonly List<CartItem> items = new();

    public IReadOnlyList<CartItem> GetItems() => items.AsReadOnly();

    public void Add(Product product, int quantity = 1)
    {
        if (product is null) throw new ArgumentNullException(nameof(product));
        if (quantity <= 0) return;

        var existing = items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            items.Add(new CartItem { Product = product, Quantity = quantity });
        }
    }

    public void Remove(int productId)
    {
        var existing = items.FirstOrDefault(i => i.Product.Id == productId);
        if (existing != null) items.Remove(existing);
    }

    public decimal GetTotal()
    {
        return items.Sum(i => i.Product.Price * i.Quantity);
    }

    public void Clear() => items.Clear();
}
