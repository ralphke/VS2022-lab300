using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace AgentGateway;

[McpServerToolType]
public sealed class TinyShopMcpTools(IHttpClientFactory httpClientFactory, IOptions<AgentGatewayOptions> options)
{
    private async Task<JsonNode?> DispatchOperationAsync(
        string operation, int customerId, Dictionary<string, object?> extra)
    {
        var config = options.Value;
        var client = httpClientFactory.CreateClient("ProductsAgent");

        using var downstreamRequest = BuildDownstreamRequest(operation, customerId, extra, config);
        if (downstreamRequest is null)
        {
            return JsonNode.Parse($"{{\"error\": \"Unsupported operation: {operation}\"}}");
        }

        downstreamRequest.Headers.Add("X-Agent-Id", config.AgentId);
        downstreamRequest.Headers.Add("X-Agent-Key", config.ProductsAgentApiKey);
        downstreamRequest.Headers.Add("X-On-Behalf-Of", customerId.ToString());
        downstreamRequest.Headers.Add("X-Agent-Request-Id", Guid.NewGuid().ToString("N"));

        try
        {
            var response = await client.SendAsync(downstreamRequest);
            var body = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body);
        }
        catch (Exception ex)
        {
            return JsonNode.Parse($"{{\"error\": \"{ex.Message}\"}}");
        }
    }

    private static HttpRequestMessage? BuildDownstreamRequest(
        string operation, int customerId, Dictionary<string, object?> extra, AgentGatewayOptions config)
    {
        int page = extra.TryGetValue("page", out var p) && p is not null ? Convert.ToInt32(p) : 1;
        int size = extra.TryGetValue("size", out var s) && s is not null ? Convert.ToInt32(s) : 5;
        int? productId = extra.TryGetValue("productId", out var pid) && pid is not null ? Convert.ToInt32(pid) : null;
        string? cartId = extra.TryGetValue("cartId", out var cid) ? cid?.ToString() : null;
        int? quantity = extra.TryGetValue("quantity", out var q) && q is not null ? Convert.ToInt32(q) : null;
        int ttlMinutes = extra.TryGetValue("ttlMinutes", out var ttl) && ttl is not null ? Convert.ToInt32(ttl) : 30;
        string? keyword = extra.TryGetValue("keyword", out var kw) ? kw?.ToString() : null;
        string? query = extra.TryGetValue("query", out var qry) ? qry?.ToString() : null;

        return operation switch
        {
            "list_products" => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/products?page={page}&size={size}"),
            "search_products" when !string.IsNullOrWhiteSpace(keyword) => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/products/search/keyword?keyword={Uri.EscapeDataString(keyword)}&page={page}&size={size}"),
            "semantic_search" when !string.IsNullOrWhiteSpace(query) => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/products/search/semantic?query={Uri.EscapeDataString(query)}&page={page}&size={size}"),
            "get_product" when productId.HasValue => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/products/{productId.Value}"),
            "create_cart" => new HttpRequestMessage(HttpMethod.Post, "/api/agent/carts")
            {
                Content = JsonContent.Create(new { customerId, ttlMinutes })
            },
            "get_cart" when cartId is not null => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/carts/{cartId}"),
            "add_or_update_item" when cartId is not null && productId.HasValue && quantity.HasValue => new HttpRequestMessage(HttpMethod.Put, $"/api/agent/carts/{cartId}/items")
            {
                Content = JsonContent.Create(new { productId = productId.Value, quantity = quantity.Value })
            },
            "remove_item" when cartId is not null && productId.HasValue => new HttpRequestMessage(HttpMethod.Delete, $"/api/agent/carts/{cartId}/items/{productId.Value}"),
            "checkout_cart" when cartId is not null => new HttpRequestMessage(HttpMethod.Post, $"/api/agent/carts/{cartId}/checkout"),
            "list_orders" => new HttpRequestMessage(HttpMethod.Get, "/api/agent/orders"),
            _ => null
        };
    }

    [McpServerTool, Description("List products from the TinyShop catalog with optional pagination.")]
    public async Task<JsonNode?> ListProducts(
        [Description("Customer ID (required for agent API routing, default 1)")] int customerId = 1,
        [Description("Page number (1-based, default 1)")] int page = 1,
        [Description("Number of products per page (default 5, max 5)")] int size = 5)
    {
        return await DispatchOperationAsync("list_products", customerId, new() { ["page"] = page, ["size"] = size });
    }

    [McpServerTool, Description("Search for products by keyword. Returns results with pagination (max 5 per page).")]
    public async Task<JsonNode?> SearchProductsByKeyword(
        [Description("Search keyword (e.g., 'flashlight', 'camping')")] string keyword,
        [Description("Customer ID (required for agent API routing, default 1)")] int customerId = 1,
        [Description("Page number (1-based, default 1)")] int page = 1)
    {
        return await DispatchOperationAsync("search_products", customerId, new() { ["keyword"] = keyword, ["page"] = page, ["size"] = 5 });
    }

    [McpServerTool, Description("Search for products using semantic/AI-powered search. Understands natural language queries like 'I need a light for camping'. Returns results with pagination.")]
    public async Task<JsonNode?> SearchProductsBySemantic(
        [Description("Natural language search query (e.g., 'outdoor lighting for hiking', 'camping gear')")] string query,
        [Description("Customer ID (required for agent API routing, default 1)")] int customerId = 1,
        [Description("Page number (1-based, default 1)")] int page = 1)
    {
        return await DispatchOperationAsync("semantic_search", customerId, new() { ["query"] = query, ["page"] = page, ["size"] = 5 });
    }

    [McpServerTool, Description("Get details of a single product by its ID.")]
    public async Task<JsonNode?> GetProduct(
        [Description("The numeric product ID")] int productId,
        [Description("Customer ID (required for agent API routing, default 1)")] int customerId = 1)
    {
        return await DispatchOperationAsync("get_product", customerId, new() { ["productId"] = productId });
    }

    [McpServerTool, Description("Create a new shopping cart for a customer.")]
    public async Task<JsonNode?> CreateCart(
        [Description("The customer ID who owns the cart")] int customerId,
        [Description("How long to keep the cart alive in minutes (default 30)")] int ttlMinutes = 30)
    {
        return await DispatchOperationAsync("create_cart", customerId, new() { ["ttlMinutes"] = ttlMinutes });
    }

    [McpServerTool, Description("Get the contents of an existing shopping cart.")]
    public async Task<JsonNode?> GetCart(
        [Description("The customer ID")] int customerId,
        [Description("The cart GUID")] string cartId)
    {
        return await DispatchOperationAsync("get_cart", customerId, new() { ["cartId"] = cartId });
    }

    [McpServerTool, Description("Add a product to a cart, or update its quantity. Set quantity to 0 to remove.")]
    public async Task<JsonNode?> AddOrUpdateCartItem(
        [Description("The customer ID")] int customerId,
        [Description("The cart GUID")] string cartId,
        [Description("The product ID to add or update")] int productId,
        [Description("The desired quantity (0 removes the item)")] int quantity)
    {
        return await DispatchOperationAsync("add_or_update_item", customerId, new() { ["cartId"] = cartId, ["productId"] = productId, ["quantity"] = quantity });
    }

    [McpServerTool, Description("Remove a specific product from a cart.")]
    public async Task<JsonNode?> RemoveCartItem(
        [Description("The customer ID")] int customerId,
        [Description("The cart GUID")] string cartId,
        [Description("The product ID to remove")] int productId)
    {
        return await DispatchOperationAsync("remove_item", customerId, new() { ["cartId"] = cartId, ["productId"] = productId });
    }

    [McpServerTool, Description("Checkout a cart and place the order.")]
    public async Task<JsonNode?> CheckoutCart(
        [Description("The customer ID")] int customerId,
        [Description("The cart GUID to checkout")] string cartId)
    {
        return await DispatchOperationAsync("checkout_cart", customerId, new() { ["cartId"] = cartId });
    }

    [McpServerTool, Description("List all orders for a customer.")]
    public async Task<JsonNode?> ListOrders(
        [Description("The customer ID whose orders to list")] int customerId)
    {
        return await DispatchOperationAsync("list_orders", customerId, new());
    }
}

