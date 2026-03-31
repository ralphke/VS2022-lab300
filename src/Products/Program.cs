using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Products.Data;
using Products.Endpoints;
using Products.Services;

var builder = WebApplication.CreateBuilder(args);
var enableHttpsRedirection = builder.Configuration.GetValue("EnableHttpsRedirection", true);

builder.AddServiceDefaults();

builder.Services.Configure<AgentAccessOptions>(builder.Configuration.GetSection(AgentAccessOptions.SectionName));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("agent-api", context =>
    {
        var configuredLimit = context.RequestServices.GetRequiredService<IOptions<AgentAccessOptions>>().Value.RequestsPerMinute;
        var permitLimit = configuredLimit <= 0 ? 60 : configuredLimit;
        var partitionKey = context.Request.Headers["X-Agent-Id"].ToString();
        partitionKey = string.IsNullOrWhiteSpace(partitionKey) ? "anonymous-agent" : partitionKey;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// Configure SQL Server connection
var connectionString = builder.Configuration.GetConnectionString("ProductsDb") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=TineShopDB;Integrated Security=true;TrustServerCertificate=True;";

builder.Services.AddDbContext<ProductDataContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("ProductsTestDb");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Add embedding service for semantic search
builder.Services.AddHttpClient<IEmbeddingService, LocalEmbeddingService>();
builder.Services.AddScoped<ProductSearchService>();

// Add CORS policy for Blazor Server frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.AllowAnyOrigin()
  .AllowAnyMethod()
          .AllowAnyHeader();
    });
});

// Add services to the container.
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// Enable CORS before other middleware
app.UseCors("AllowBlazorClient");
app.UseRateLimiter();

// Static files must be configured before routing
app.UseStaticFiles();

app.MapProductEndpoints();
app.MapCustomerOrderEndpoints();
app.MapAgentCommerceEndpoints();

// Initialize database and seed data if needed
await app.InitializeDatabaseAsync();

app.Run();

namespace Products
{
    // Expose Program for integration tests
    public partial class Program { }
}