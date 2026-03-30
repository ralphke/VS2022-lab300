using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure SQL Server connection
var connectionString = builder.Configuration.GetConnectionString("ProductsDb") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=TestDB;Integrated Security=true;TrustServerCertificate=True;";

builder.Services.AddDbContext<ProductDataContext>(options =>
    options.UseSqlServer(connectionString));

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
app.UseHttpsRedirection();

// Enable CORS before other middleware
app.UseCors("AllowBlazorClient");

// Static files must be configured before routing
app.UseStaticFiles();

app.MapProductEndpoints();

// Initialize database and seed data if needed
await app.InitializeDatabaseAsync();

app.Run();

namespace Products
{
    // Expose Program for integration tests
    public partial class Program { }
}