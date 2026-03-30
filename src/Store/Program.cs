using Store.Components;
using Store.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register ProductService as an HTTP typed client so injected HttpClient is configured from configuration
// (do not register as a separate singleton which conflicts with AddHttpClient).
builder.Services.AddHttpClient<ProductService>(c =>
{
    var url = builder.Configuration["ProductEndpoint"] ?? "https://localhost:7130";
    c.BaseAddress = new(url);
});

// Add cart service (scoped to Blazor circuit) and circuit handler
builder.Services.AddScoped<CartService>();
builder.Services.AddSingleton<CircuitHandler, CartCircuitHandler>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMemoryCache();
// Register CartService as scoped so each user/session gets its own cart
builder.Services.AddScoped<CartService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

namespace Store
{
    public partial class Program { }
}
