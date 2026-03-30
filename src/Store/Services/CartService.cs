using DataEntities;

namespace Store.Services;

public class CartService : IDisposable
{
    private readonly Dictionary<int, CartItem> _items = new();
    private bool _disposed = false;

    public event Action? OnChange;

    public IReadOnlyDictionary<int, CartItem> Items => _items;

    public void AddItem(Product product, int quantity = 1)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (_items.ContainsKey(product.Id))
        {
            _items[product.Id].Quantity += quantity;
        }
        else
        {
            _items[product.Id] = new CartItem
            {
                Product = product,
                Quantity = quantity
            };
        }

        NotifyStateChanged();
    }

    public void RemoveItem(int productId, int quantity = 1)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (_items.ContainsKey(productId))
        {
            _items[productId].Quantity -= quantity;

            if (_items[productId].Quantity <= 0)
            {
                _items.Remove(productId);
            }

            NotifyStateChanged();
        }
    }

    public void RemoveAll(int productId)
    {
        if (_items.Remove(productId))
        {
            NotifyStateChanged();
        }
    }

    public decimal GetTotal()
    {
        return _items.Values.Sum(item => item.Product.Price * item.Quantity);
    }

    public void Clear()
    {
        if (_items.Count > 0)
        {
            _items.Clear();
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }
}

public class CartItem
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
