using System.Diagnostics.Metrics;
using DataEntities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Products.Data;
using Products.Services;

namespace Products.Endpoints;

public static class AgentCommerceEndpoints
{
    private const string AgentIdHeader = "X-Agent-Id";
    private const string AgentKeyHeader = "X-Agent-Key";
    private const string OnBehalfOfHeader = "X-On-Behalf-Of";
    private const string RequestIdHeader = "X-Agent-Request-Id";

    private static readonly Meter AgentMeter = new("TinyShop.AgentCommerce", "1.0.0");
    private static readonly Counter<long> AgentRequests = AgentMeter.CreateCounter<long>("agent_requests_total");
    private static readonly Counter<long> AgentDeniedRequests = AgentMeter.CreateCounter<long>("agent_requests_denied_total");
    private static readonly Counter<long> AgentCartMutations = AgentMeter.CreateCounter<long>("agent_cart_mutations_total");
    private static readonly Counter<long> AgentCheckoutAttempts = AgentMeter.CreateCounter<long>("agent_checkout_attempts_total");

    public static void MapAgentCommerceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/agent")
            .RequireRateLimiting("agent-api");

        group.MapGet("/capabilities", GetCapabilities)
            .WithName("GetAgentCapabilities")
            .Produces<AgentCapabilitiesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/products", GetProducts)
            .WithName("AgentGetProducts")
            .Produces<List<Product>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/products/{productId:int}", GetProductById)
            .WithName("AgentGetProductById")
            .Produces<Product>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/products/search/keyword", SearchProductsByKeyword)
            .WithName("AgentSearchByKeyword")
            .Produces<List<Product>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/products/search/semantic", SearchProductsBySemantic)
            .WithName("AgentSearchBySemantic")
            .Produces<List<Product>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/carts", CreateCart)
            .WithName("AgentCreateCart")
            .Produces<AgentCartSnapshot>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/carts/{cartId:guid}", GetCart)
            .WithName("AgentGetCart")
            .Produces<AgentCartSnapshot>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/carts/{cartId:guid}/items", UpsertCartItem)
            .WithName("AgentUpsertCartItem")
            .Produces<AgentCartSnapshot>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/carts/{cartId:guid}/items/{productId:int}", RemoveCartItem)
            .WithName("AgentRemoveCartItem")
            .Produces<AgentCartSnapshot>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/carts/{cartId:guid}/checkout", CheckoutCart)
            .WithName("AgentCheckoutCart")
            .Produces<Order>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/orders", GetOrders)
            .WithName("AgentGetOrders")
            .Produces<List<Order>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetCapabilities(HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        AgentRequests.Add(1);

        var response = new AgentCapabilitiesResponse
        {
            Tools =
            [
                new() { Name = "list_products", Description = "Get paginated products for shopping", Method = "GET", Path = "/api/agent/products?page=1&size=12" },
                new() { Name = "get_product", Description = "Get product details by id", Method = "GET", Path = "/api/agent/products/{productId}" },
                new() { Name = "create_cart", Description = "Create a persistent agent cart for a customer", Method = "POST", Path = "/api/agent/carts" },
                new() { Name = "upsert_cart_item", Description = "Add or update a cart item", Method = "PUT", Path = "/api/agent/carts/{cartId}/items" },
                new() { Name = "remove_cart_item", Description = "Remove an item from the cart", Method = "DELETE", Path = "/api/agent/carts/{cartId}/items/{productId}" },
                new() { Name = "checkout_cart", Description = "Checkout a cart and place an order", Method = "POST", Path = "/api/agent/carts/{cartId}/checkout" },
                new() { Name = "list_orders", Description = "List customer orders", Method = "GET", Path = "/api/agent/orders" }
            ]
        };

        await WriteAuditAsync(db, caller, "capabilities", StatusCodes.Status200OK);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetProducts(HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options, int? page = null, int? size = null)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        AgentRequests.Add(1);

        var query = db.Product.AsNoTracking().OrderBy(product => product.Id);

        if (page.HasValue && size.HasValue && page.Value > 0 && size.Value > 0)
        {
            query = (IOrderedQueryable<Product>)query.Skip((page.Value - 1) * size.Value).Take(size.Value);
        }

        var products = await query.ToListAsync();
        await WriteAuditAsync(db, caller, "list_products", StatusCodes.Status200OK);
        return Results.Ok(products);
    }

    private static async Task<IResult> GetProductById(int productId, HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        AgentRequests.Add(1);

        var product = await db.Product.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == productId);
        if (product is null)
        {
            await WriteAuditAsync(db, caller, "get_product", StatusCodes.Status404NotFound);
            return Results.NotFound();
        }

        await WriteAuditAsync(db, caller, "get_product", StatusCodes.Status200OK);
        return Results.Ok(product);
    }

    private static async Task<IResult> SearchProductsByKeyword(HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options, ProductSearchService searchService, string? keyword = null, int? page = null)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        AgentRequests.Add(1);

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Results.BadRequest(new { message = "keyword query parameter is required." });
        }

        var pageNum = page ?? 1;
        var result = await searchService.SearchByKeywordAsync(keyword, pageNum, 5);
        
        await WriteAuditAsync(db, caller, "search_products_keyword", StatusCodes.Status200OK);
        
        return Results.Ok(new
        {
            items = result.Products,
            total = result.Total,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }

    private static async Task<IResult> SearchProductsBySemantic(HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options, ProductSearchService searchService, string? query = null, int? page = null)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        AgentRequests.Add(1);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new { message = "query parameter is required." });
        }

        var pageNum = page ?? 1;
        var result = await searchService.SearchBySemanticAsync(query, pageNum, 5);
        
        await WriteAuditAsync(db, caller, "search_products_semantic", StatusCodes.Status200OK);
        
        return Results.Ok(new
        {
            items = result.Products,
            total = result.Total,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }

    private static async Task<IResult> CreateCart(AgentCreateCartRequest request, HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        if (request.CustomerId != caller.CustomerId)
        {
            return Results.BadRequest(new { message = "Request customer does not match on-behalf-of identity." });
        }

        var customerExists = await db.Customers.AnyAsync(customer => customer.Id == request.CustomerId);
        if (!customerExists)
        {
            await WriteAuditAsync(db, caller, "create_cart", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Customer not found." });
        }

        var now = DateTime.UtcNow;
        var session = new AgentCartSession
        {
            Id = Guid.NewGuid(),
            AgentId = caller.AgentId,
            CustomerId = request.CustomerId,
            CreatedDate = now,
            LastActivityDate = now,
            ExpiresAt = now.AddMinutes(request.TtlMinutes)
        };

        db.AgentCartSessions.Add(session);
        await db.SaveChangesAsync();

        AgentCartMutations.Add(1);
        await WriteAuditAsync(db, caller, "create_cart", StatusCodes.Status201Created);

        return Results.Created($"/api/agent/carts/{session.Id}", ToSnapshot(session));
    }

    private static async Task<IResult> GetCart(Guid cartId, HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        var cart = await db.AgentCartSessions
            .Include(session => session.Items)
            .FirstOrDefaultAsync(session => session.Id == cartId && session.AgentId == caller.AgentId && session.CustomerId == caller.CustomerId);

        if (cart is null || cart.ExpiresAt <= DateTime.UtcNow)
        {
            await WriteAuditAsync(db, caller, "get_cart", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Cart not found or expired." });
        }

        cart.LastActivityDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        AgentRequests.Add(1);
        await WriteAuditAsync(db, caller, "get_cart", StatusCodes.Status200OK);
        return Results.Ok(ToSnapshot(cart));
    }

    private static async Task<IResult> UpsertCartItem(Guid cartId, AgentUpsertCartItemRequest request, HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        var cart = await db.AgentCartSessions
            .Include(session => session.Items)
            .FirstOrDefaultAsync(session => session.Id == cartId && session.AgentId == caller.AgentId && session.CustomerId == caller.CustomerId);

        if (cart is null || cart.ExpiresAt <= DateTime.UtcNow)
        {
            await WriteAuditAsync(db, caller, "upsert_cart_item", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Cart not found or expired." });
        }

        var product = await db.Product.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == request.ProductId);
        if (product is null)
        {
            await WriteAuditAsync(db, caller, "upsert_cart_item", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Product not found." });
        }

        var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == request.ProductId);
        if (existingItem is null)
        {
            cart.Items.Add(new AgentCartItem
            {
                ProductId = product.Id,
                ProductName = product.Name ?? $"Product {product.Id}",
                UnitPrice = product.Price,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity = request.Quantity;
            existingItem.ProductName = product.Name ?? existingItem.ProductName;
            existingItem.UnitPrice = product.Price;
        }

        cart.LastActivityDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        AgentCartMutations.Add(1);
        await WriteAuditAsync(db, caller, "upsert_cart_item", StatusCodes.Status200OK);
        return Results.Ok(ToSnapshot(cart));
    }

    private static async Task<IResult> RemoveCartItem(Guid cartId, int productId, HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        var cart = await db.AgentCartSessions
            .Include(session => session.Items)
            .FirstOrDefaultAsync(session => session.Id == cartId && session.AgentId == caller.AgentId && session.CustomerId == caller.CustomerId);

        if (cart is null || cart.ExpiresAt <= DateTime.UtcNow)
        {
            await WriteAuditAsync(db, caller, "remove_cart_item", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Cart not found or expired." });
        }

        var item = cart.Items.FirstOrDefault(candidate => candidate.ProductId == productId);
        if (item is null)
        {
            await WriteAuditAsync(db, caller, "remove_cart_item", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Cart item not found." });
        }

        db.AgentCartItems.Remove(item);
        cart.LastActivityDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        AgentCartMutations.Add(1);
        await WriteAuditAsync(db, caller, "remove_cart_item", StatusCodes.Status200OK);
        return Results.Ok(ToSnapshot(cart));
    }

    private static async Task<IResult> CheckoutCart(Guid cartId, HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        var cart = await db.AgentCartSessions
            .Include(session => session.Items)
            .FirstOrDefaultAsync(session => session.Id == cartId && session.AgentId == caller.AgentId && session.CustomerId == caller.CustomerId);

        if (cart is null || cart.ExpiresAt <= DateTime.UtcNow)
        {
            await WriteAuditAsync(db, caller, "checkout_cart", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Cart not found or expired." });
        }

        if (cart.Items.Count == 0)
        {
            await WriteAuditAsync(db, caller, "checkout_cart", StatusCodes.Status400BadRequest);
            return Results.BadRequest(new { message = "Cart is empty." });
        }

        var customer = await db.Customers.FindAsync(caller.CustomerId);
        if (customer is null)
        {
            await WriteAuditAsync(db, caller, "checkout_cart", StatusCodes.Status404NotFound);
            return Results.NotFound(new { message = "Customer not found." });
        }

        var productIds = cart.Items.Select(item => item.ProductId).Distinct().ToList();
        var existingProducts = await db.Product
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        var missingProductIds = productIds.Where(id => !existingProducts.ContainsKey(id)).ToList();
        if (missingProductIds.Count > 0)
        {
            await WriteAuditAsync(db, caller, "checkout_cart", StatusCodes.Status400BadRequest);
            return Results.BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
        }

        var order = new Order
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            ShippingAddress = customer.Address,
            Status = OrderStatuses.Completed,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            var product = existingProducts[item.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name ?? item.ProductName,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        order.Total = order.Items.Sum(item => item.UnitPrice * item.Quantity);

        db.Orders.Add(order);
        db.AgentCartItems.RemoveRange(cart.Items);
        db.AgentCartSessions.Remove(cart);
        await db.SaveChangesAsync();

        AgentCheckoutAttempts.Add(1);
        await WriteAuditAsync(db, caller, "checkout_cart", StatusCodes.Status201Created);
        return Results.Created($"/api/orders/{order.Id}", order);
    }

    private static async Task<IResult> GetOrders(HttpContext httpContext, ProductDataContext db, IOptions<AgentAccessOptions> options)
    {
        if (!TryGetCallerContext(httpContext, options.Value, out var caller, out var errorResult))
        {
            AgentDeniedRequests.Add(1);
            return errorResult;
        }

        var orders = await db.Orders
            .Where(order => order.CustomerId == caller.CustomerId)
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedDate)
            .ToListAsync();

        AgentRequests.Add(1);
        await WriteAuditAsync(db, caller, "list_orders", StatusCodes.Status200OK);
        return Results.Ok(orders);
    }

    private static bool TryGetCallerContext(HttpContext httpContext, AgentAccessOptions options, out AgentCallerContext caller, out IResult errorResult)
    {
        caller = default!;
        errorResult = Results.Unauthorized();

        var agentId = httpContext.Request.Headers[AgentIdHeader].ToString();
        var apiKey = httpContext.Request.Headers[AgentKeyHeader].ToString();
        var customerHeader = httpContext.Request.Headers[OnBehalfOfHeader].ToString();
        var requestId = httpContext.Request.Headers[RequestIdHeader].ToString();

        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(customerHeader))
        {
            errorResult = Results.BadRequest(new
            {
                message = $"Missing required headers. Required: {AgentIdHeader}, {AgentKeyHeader}, {OnBehalfOfHeader}."
            });
            return false;
        }

        if (!int.TryParse(customerHeader, out var customerId) || customerId <= 0)
        {
            errorResult = Results.BadRequest(new { message = $"Header {OnBehalfOfHeader} must be a valid customer id." });
            return false;
        }

        var credential = options.Agents.FirstOrDefault(candidate =>
            candidate.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase)
            && candidate.ApiKey == apiKey);

        if (credential is null)
        {
            errorResult = Results.Unauthorized();
            return false;
        }

        caller = new AgentCallerContext(
            credential.AgentId,
            customerId,
            string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId.Trim());
        return true;
    }

    private static AgentCartSnapshot ToSnapshot(AgentCartSession session)
    {
        var items = session.Items
            .OrderBy(item => item.ProductName)
            .Select(item => new AgentCartSnapshotItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            })
            .ToList();

        return new AgentCartSnapshot
        {
            CartId = session.Id,
            CustomerId = session.CustomerId,
            AgentId = session.AgentId,
            CreatedDate = session.CreatedDate,
            ExpiresAt = session.ExpiresAt,
            Items = items,
            Total = items.Sum(item => item.UnitPrice * item.Quantity)
        };
    }

    private static async Task WriteAuditAsync(ProductDataContext db, AgentCallerContext caller, string operation, int statusCode)
    {
        db.AgentRequestAudits.Add(new AgentRequestAudit
        {
            AgentId = caller.AgentId,
            CustomerId = caller.CustomerId,
            Operation = operation,
            RequestId = caller.RequestId,
            StatusCode = statusCode,
            CreatedDate = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private sealed record AgentCallerContext(string AgentId, int CustomerId, string RequestId);
}
