using System.Net.Http.Json;
using System.Text.Json;
using AgentGateway;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var enableHttpsRedirection = builder.Configuration.GetValue("EnableHttpsRedirection", true);

builder.AddServiceDefaults();
builder.Services.Configure<AgentGatewayOptions>(builder.Configuration.GetSection(AgentGatewayOptions.SectionName));

var productsEndpoint = builder.Configuration["ProductsEndpoint"] ?? "https+http://products";
builder.Services.AddHttpClient("ProductsAgent", client =>
{
    client.BaseAddress = new Uri(productsEndpoint);
});

builder.Services.AddScoped<TinyShopMcpTools>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapMcp("/mcp");

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

var group = app.MapGroup("/api/agent-gateway");

group.MapGet("/agent-card", (IOptions<AgentGatewayOptions> options) =>
{
    var config = options.Value;
    var card = new
    {
        name = "TinyShop Local Agent Gateway",
        version = "0.1.0",
        protocol = "a2a-compatible-rest-adapter",
        requiresApiKeyHeader = AgentGatewayOptions.ApiKeyHeaderName,
        skills = new[]
        {
            "list_products",
            "get_product",
            "create_cart",
            "get_cart",
            "add_or_update_item",
            "remove_item",
            "checkout_cart",
            "list_orders"
        },
        security = new
        {
            acceptedApiKey = "dev-change-me (development only)",
            onBehalfOf = "customerId in request payload"
        },
        downstream = "/api/agent (Products service)"
    };

    return Results.Ok(card);
});

group.MapPost("/tasks", async (AgentTaskRequest request, HttpRequest httpRequest, IHttpClientFactory clientFactory, IOptions<AgentGatewayOptions> options) =>
{
    var config = options.Value;
    var incomingApiKey = httpRequest.Headers[AgentGatewayOptions.ApiKeyHeaderName].ToString();

    if (string.IsNullOrWhiteSpace(incomingApiKey) || incomingApiKey != config.ApiKey)
    {
        return Results.Unauthorized();
    }

    if (request.CustomerId <= 0)
    {
        return Results.BadRequest(new { message = "customerId must be provided and greater than 0." });
    }

    var operation = request.Operation?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(operation))
    {
        return Results.BadRequest(new { message = "operation is required." });
    }

    var client = clientFactory.CreateClient("ProductsAgent");
    using var downstreamRequest = BuildDownstreamRequest(operation, request, config);

    if (downstreamRequest is null)
    {
        return Results.BadRequest(new { message = "Unsupported operation." });
    }

    downstreamRequest.Headers.Add("X-Agent-Id", config.AgentId);
    downstreamRequest.Headers.Add("X-Agent-Key", config.ProductsAgentApiKey);
    downstreamRequest.Headers.Add("X-On-Behalf-Of", request.CustomerId.ToString());
    downstreamRequest.Headers.Add("X-Agent-Request-Id", string.IsNullOrWhiteSpace(request.RequestId) ? Guid.NewGuid().ToString("N") : request.RequestId);

    var response = await client.SendAsync(downstreamRequest);
    var body = await response.Content.ReadAsStringAsync();

    return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
});

app.Run();

static HttpRequestMessage? BuildDownstreamRequest(string operation, AgentTaskRequest request, AgentGatewayOptions options)
{
    return operation switch
    {
        "list_products" => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/products?page={request.Page ?? 1}&size={request.Size ?? 12}"),
        "get_product" when request.ProductId.HasValue => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/products/{request.ProductId.Value}"),
        "create_cart" => new HttpRequestMessage(HttpMethod.Post, "/api/agent/carts")
        {
            Content = JsonContent.Create(new
            {
                customerId = request.CustomerId,
                ttlMinutes = request.TtlMinutes ?? 30
            })
        },
        "get_cart" when request.CartId.HasValue => new HttpRequestMessage(HttpMethod.Get, $"/api/agent/carts/{request.CartId.Value}"),
        "add_or_update_item" when request.CartId.HasValue && request.ProductId.HasValue && request.Quantity.HasValue => new HttpRequestMessage(HttpMethod.Put, $"/api/agent/carts/{request.CartId.Value}/items")
        {
            Content = JsonContent.Create(new
            {
                productId = request.ProductId.Value,
                quantity = request.Quantity.Value
            })
        },
        "remove_item" when request.CartId.HasValue && request.ProductId.HasValue => new HttpRequestMessage(HttpMethod.Delete, $"/api/agent/carts/{request.CartId.Value}/items/{request.ProductId.Value}"),
        "checkout_cart" when request.CartId.HasValue => new HttpRequestMessage(HttpMethod.Post, $"/api/agent/carts/{request.CartId.Value}/checkout"),
        "list_orders" => new HttpRequestMessage(HttpMethod.Get, "/api/agent/orders"),
        _ => null
    };
}

public sealed class AgentTaskRequest
{
    public string? Operation { get; set; }

    public string? RequestId { get; set; }

    public int CustomerId { get; set; }

    public Guid? CartId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public int? Page { get; set; }

    public int? Size { get; set; }

    public int? TtlMinutes { get; set; }
}

public sealed class AgentGatewayOptions
{
    public const string SectionName = "AgentGateway";
    public const string ApiKeyHeaderName = "X-Dev-Api-Key";

    public string ApiKey { get; set; } = "dev-change-me";

    public string AgentId { get; set; } = "local-agent-gateway";

    public string ProductsAgentApiKey { get; set; } = "dev-change-me";
}

namespace AgentGateway
{
    public partial class Program { }
}
