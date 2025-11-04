# TinyShop Solution Guide

**Version:** 1.0  
**Target Framework:** .NET 9  
**Last Updated:** 2025

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Projects](#projects)
4. [Getting Started](#getting-started)
5. [Development Workflow](#development-workflow)
6. [Testing](#testing)
7. [Configuration](#configuration)
8. [API Reference](#api-reference)
9. [Troubleshooting](#troubleshooting)

---

## Overview

TinyShop is a modern e-commerce demonstration application built with .NET 9, showcasing best practices for building cloud-native applications using:

- **Backend API**: ASP.NET Core Minimal APIs with Entity Framework Core
- **Frontend**: Blazor Server for interactive web UI
- **Orchestration**: .NET Aspire for local development and deployment
- **Observability**: OpenTelemetry integration for monitoring and tracing
- **Testing**: Integration tests with code coverage reporting

### Key Features

- Product catalog management (CRUD operations)
- Image handling (URL-based and binary storage)
- RESTful API following OpenAPI standards
- Interactive Blazor Server UI with real-time updates
- Health checks and service discovery
- Distributed application orchestration with Aspire
- Comprehensive test coverage with HTML reporting

---

## Architecture

### High-Level Architecture

```
???????????????????????????????????????????????????????????????
?                    TinyShop.AppHost                         ?
?              (.NET Aspire Orchestrator)                     ?
?  - Launches Products API and Store UI                       ?
?  - Provides Aspire Dashboard                                ?
?  - Manages service dependencies                             ?
???????????????????????????????????????????????????????????????
                      ?
          ?????????????????????????
          ?                       ?
??????????????????????  ????????????????????
?   Products API     ?  ?   Store (UI)     ?
?  (Minimal API)     ???? (Blazor Server)  ?
?                    ?  ?                  ?
?  - EF Core         ?  ? - ProductService ?
?  - SQL Server      ?  ? - Razor Pages    ?
?  - REST Endpoints  ?  ? - Bootstrap UI   ?
??????????????????????  ????????????????????
         ?
         ? Uses
         ?
?????????????????????????????????????????????
?        TinyShop.ServiceDefaults           ?
?  - OpenTelemetry Configuration            ?
?  - Health Checks                          ?
?  - Service Discovery                      ?
?  - Standard Resilience Handlers           ?
?????????????????????????????????????????????
         ?
         ? References
         ?
?????????????????????????????????????????????
?           DataEntities                    ?
?  - Product Model                          ?
?  - JSON Source Generation                 ?
?  - Shared DTOs                            ?
?????????????????????????????????????????????
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend Framework** | ASP.NET Core 9.0 Minimal APIs |
| **Frontend Framework** | Blazor Server (Interactive Server Components) |
| **Database** | SQL Server LocalDB / Entity Framework Core 9.0 |
| **Orchestration** | .NET Aspire |
| **Observability** | OpenTelemetry (Metrics, Traces, Logs) |
| **Testing** | xUnit, WebApplicationFactory, Coverlet |
| **Reporting** | ReportGenerator (HTML Coverage Reports) |
| **Styling** | Bootstrap 5 |
| **API Documentation** | OpenAPI/Swagger |

---

## Projects

### 1. Products (Backend API)

**Location:** `Products/`  
**Type:** ASP.NET Core Minimal API  
**Purpose:** RESTful API for product catalog management

#### Key Components

##### Program.cs
- Entry point for the API application
- Configures services and middleware pipeline
- Sets up:
  - Service defaults (OpenTelemetry, health checks)
  - Entity Framework Core with SQL Server
  - CORS policy for Blazor frontend
  - Database initialization and seeding

```csharp
// Key configuration
builder.Services.AddDbContext<ProductDataContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddCors(options => {
    options.AddPolicy("AllowBlazorClient", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

##### ProductEndpoints.cs
Located in: `Products/Endpoints/`

Defines all API endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/Product` | GET | Get all products |
| `/api/Product/count` | GET | Get product count |
| `/api/Product/{id}` | GET | Get product by ID |
| `/api/Product/{id}/image` | GET | Get product image (binary PNG) |
| `/api/Product/debug/images` | GET | Debug endpoint for image configuration |
| `/api/Product` | POST | Create new product |
| `/api/Product/{id}` | PUT | Update existing product |
| `/api/Product/{id}/image` | PUT | Upload product image |
| `/api/Product/{id}` | DELETE | Delete product |

##### ProductDataContext.cs
Located in: `Products/Data/`

- Entity Framework Core DbContext
- Configures `Product` entity for SQL Server
- Handles database initialization and seeding
- `InitializeDatabaseAsync()` - Ensures database exists and seeds initial data
- `DbInitializer.InitializeAsync()` - Seeds 9 sample outdoor products

**Database Schema:**
```sql
Table: Products (dbo)
- Id (int, PK, Identity)
- Name (nvarchar(200), Required, Indexed)
- Description (nvarchar(1000))
- Price (decimal(18,2))
- ImageUrl (nvarchar(500))
- ImageData (varbinary(max))
- CreatedDate (datetime, default: GETUTCDATE())
- ModifiedDate (datetime, default: GETUTCDATE())
```

#### Dependencies
- `Microsoft.EntityFrameworkCore.SqlServer`
- `TinyShop.ServiceDefaults`
- `DataEntities`

---

### 2. Store (Frontend UI)

**Location:** `Store/`  
**Type:** Blazor Server Application  
**Purpose:** Interactive web UI for browsing and managing products

#### Key Components

##### Program.cs
- Entry point for the Blazor application
- Configures services:
  - Service defaults
  - HTTP client for ProductService
  - Razor components with interactive server mode
- Sets up middleware pipeline

```csharp
// ProductService registration
builder.Services.AddHttpClient<ProductService>(c => {
    var url = builder.Configuration["ProductEndpoint"] 
        ?? "https://localhost:7130";
    c.BaseAddress = new(url);
});

// Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

##### ProductService.cs
Located in: `Store/Services/`

HTTP client wrapper for Products API:

| Method | Description |
|--------|-------------|
| `GetProducts()` | Fetch all products from API |
| `GetProductsPageAsync(int count)` | Fetch limited number of products |
| `GetProductByIdAsync(int id)` | Fetch single product |
| `CreateProductAsync(Product)` | Create new product |
| `UpdateProductAsync(int id, Product)` | Update existing product |
| `DeleteProductAsync(int id)` | Delete product |
| `GetImageUrl(Product)` | Generate image URL for product display |

##### Razor Components
Located in: `Store/Components/Pages/`

**Home.razor** (`/`)
- Landing page with featured products
- Displays first 3 products from catalog
- "Shop Now" call-to-action button
- Error handling for missing products

**Products.razor** (`/products`)
- Product catalog grid view
- Displays all products with images
- Loading spinner during data fetch
- "Add to Cart" placeholder buttons
- Responsive grid layout (3 columns)

**ProductsDebug.razor** (`/products-debug`)
- Diagnostic page for image configuration
- Shows image URL vs. binary data status
- API endpoint debugging information
- Useful for troubleshooting image issues

**About.razor** (`/about`)
- About page with query parameter validation
- Example of navigation and routing

##### Layout Components
Located in: `Store/Components/Layout/`

**MainLayout.razor**
- Main application layout
- Navigation sidebar
- Content area
- Consistent styling across pages

#### Styling
- **Bootstrap 5**: Default styling framework
- **Scoped CSS**: Component-specific styles in `.razor.css` files
- **Custom Styles**: `wwwroot/app.css` for global styles

#### Dependencies
- `TinyShop.ServiceDefaults`
- `DataEntities`

---

### 3. DataEntities (Shared Models)

**Location:** `DataEntities/`  
**Type:** Class Library  
**Purpose:** Shared data models and DTOs

#### Product.cs

Core product entity:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public byte[]? ImageData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    // Computed property
    public string? ImageDataBase64 
    { 
        get => ImageData != null 
            ? Convert.ToBase64String(ImageData) 
            : null;
        set => ImageData = value != null 
            ? Convert.FromBase64String(value) 
            : null;
    }
}
```

#### JSON Source Generation

**ProductSerializerContext.cs**
- Uses `System.Text.Json` source generation
- Optimized serialization for `Product`, `List<Product>`
- Reduces reflection overhead
- Improves startup performance

```csharp
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(List<Product>))]
public partial class ProductSerializerContext : JsonSerializerContext
{
}
```

---

### 4. TinyShop.ServiceDefaults (Shared Configuration)

**Location:** `TinyShop.ServiceDefaults/`  
**Type:** Class Library  
**Purpose:** Common service configurations

#### Extensions.cs

Provides extension methods for consistent service configuration:

##### `AddServiceDefaults<TBuilder>()`
- Configures OpenTelemetry
- Adds default health checks
- Enables service discovery
- Configures HTTP client defaults with resilience

##### `ConfigureOpenTelemetry<TBuilder>()`
- **Logging**: Includes formatted messages and scopes
- **Metrics**: 
  - ASP.NET Core instrumentation
  - HTTP client instrumentation
  - Runtime instrumentation
- **Tracing**:
  - ASP.NET Core instrumentation
  - HTTP client instrumentation
  - Filters out health check requests

##### `AddDefaultHealthChecks<TBuilder>()`
- Adds "self" liveness check
- Tags for readiness vs. liveness separation

##### `MapDefaultEndpoints(WebApplication)`
- Maps health check endpoints (Development only)
- `/health` - All health checks
- `/alive` - Liveness checks only

#### Configuration

```csharp
// Usage in Program.cs
builder.AddServiceDefaults();
```

**Includes:**
- Service discovery
- Standard resilience handler
- OpenTelemetry exporters (OTLP, Azure Monitor ready)

---

### 5. TinyShop.AppHost (Orchestrator)

**Location:** `TinyShop.AppHost/`  
**Type:** .NET Aspire App Host  
**Purpose:** Local development orchestration

#### Program.cs

Configures distributed application:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var products = builder.AddProject<Projects.Products>("products");

builder.AddProject<Projects.Store>("store")
    .WaitFor(products)
    .WithReference(products);

builder.Build().Run();
```

**Features:**
- Launches both Products API and Store UI
- Manages dependencies (Store waits for Products)
- Provides Aspire Dashboard at startup
- Service discovery configuration
- Centralized logging and monitoring

#### Aspire Dashboard

Access at: `http://localhost:15888` (typical port)

**Capabilities:**
- View running applications
- Monitor traces, metrics, and logs
- Inspect service endpoints
- Review environment variables
- Debug distributed applications

---

### 6. Tests (Integration Tests)

**Location:** `Tests/IntegrationTests/`  
**Type:** xUnit Test Project  
**Purpose:** Integration and API testing

#### Test Classes

##### ProductApiTests.cs
Tests for Products API endpoints:

```csharp
[Fact]
public async Task GetAllProducts_ReturnsSuccessAndList()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/Product/");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var products = await response
        .Content.ReadFromJsonAsync<List<Product>>();
    products.Should().NotBeNull();
    products.Should().HaveCountGreaterThan(0);
}

[Fact]
public async Task GetProductById_ReturnsProduct_WhenExists()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/Product/1");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var product = await response
        .Content.ReadFromJsonAsync<Product>();
    product.Should().NotBeNull();
    product!.Id.Should().Be(1);
}
```

##### StoreUiTests.cs
Tests for Blazor UI integration:

- Starts Products API in-memory
- Seeds test database
- Configures Store to use in-memory Products server
- Fetches `/products` page HTML
- Validates product names appear in HTML

**Key Pattern:**
```csharp
// Override Store configuration to use test Products server
await using var storeFactory = new WebApplicationFactory<Store.Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration((context, conf) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["ProductEndpoint"] = productApiBase
            };
            conf.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<ProductService>(client =>
            {
                client.BaseAddress = new Uri(productApiBase);
            })
            .ConfigurePrimaryHttpMessageHandler(() => 
                productsFactory.Server.CreateHandler());
        });
    });
```

#### Coverage Configuration

**IntegrationTests.csproj** includes:
```xml
<PackageReference Include="coverlet.collector" Version="6.0.4">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; 
                   analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

#### Running Tests

```bash
# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" 
    --results-directory TestResults

# Generate HTML report
dotnet tool run reportgenerator 
    -reports:TestResults/*/coverage.cobertura.xml 
    -targetdir:TestResults/CoverageReport 
    -reporttypes:Html
```

#### Current Coverage (Last Run)

| Project | Line Coverage | Branch Coverage |
|---------|--------------|-----------------|
| **Overall** | **59.72%** | **35.55%** |
| TinyShop.ServiceDefaults | 94.11% | 88.88% |
| DataEntities | 76.72% | 45.31% |
| Products | 53.66% | 11.53% |
| Store (Blazor) | 30.83% | 22.22% |

**Coverage Report:** `TestResults/CoverageReport/index.html`

---

## Getting Started

### Prerequisites

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (17.12+) or **VS Code** with C# Dev Kit
- **SQL Server LocalDB** (included with Visual Studio)
- **Git** - For cloning the repository

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/dotnet-presentations/build-2025-lab300
   cd build-2025-lab300/src
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

### Running the Application

#### Option 1: Using Aspire AppHost (Recommended)

1. Set `TinyShop.AppHost` as the startup project in Visual Studio
2. Press **F5** or click **Start Debugging**
3. The Aspire Dashboard will open in your browser
4. Access applications:
   - **Products API**: `https://localhost:7130`
   - **Store UI**: `http://localhost:7085`
   - **Aspire Dashboard**: `http://localhost:15888`

#### Option 2: Running Projects Individually

**Terminal 1 - Products API:**
```bash
cd Products
dotnet run
# API runs at https://localhost:7130
```

**Terminal 2 - Store UI:**
```bash
cd Store
dotnet run
# UI runs at http://localhost:7085
```

### First-Time Setup

On first run, the application will:
1. Create the `TestDB` database in LocalDB
2. Apply database schema
3. Seed 9 sample products
4. Products will have no images initially

**To add product images:**
```powershell
# From Products/SQL directory
.\LoadImages.ps1
```

---

## Development Workflow

### Adding a New Product Endpoint

1. **Define the endpoint** in `Products/Endpoints/ProductEndpoints.cs`:
   ```csharp
   group.MapGet("/api/Product/featured", async (ProductDataContext db) =>
   {
       return await db.Product
           .Where(p => p.Price < 50)
           .Take(5)
           .ToListAsync();
   })
   .WithName("GetFeaturedProducts")
   .Produces<List<Product>>(StatusCodes.Status200OK);
   ```

2. **Add service method** in `Store/Services/ProductService.cs`:
   ```csharp
   public async Task<List<Product>> GetFeaturedProductsAsync()
   {
       var response = await _httpClient.GetAsync("api/Product/featured");
       response.EnsureSuccessStatusCode();
       return await response.Content
           .ReadFromJsonAsync<List<Product>>() ?? new();
   }
   ```

3. **Update UI component** to use the new method

4. **Add integration test**:
   ```csharp
   [Fact]
   public async Task GetFeaturedProducts_ReturnsLimitedList()
   {
       var client = _factory.CreateClient();
       var response = await client.GetAsync("/api/Product/featured");
       
       response.StatusCode.Should().Be(HttpStatusCode.OK);
       var products = await response.Content
           .ReadFromJsonAsync<List<Product>>();
       products.Should().HaveCountLessThanOrEqualTo(5);
   }
   ```

### Adding a New Blazor Page

1. **Create component** in `Store/Components/Pages/`:
   ```razor
   @page "/cart"
   @rendermode InteractiveServer
   @using DataEntities
   
   <PageTitle>Shopping Cart</PageTitle>
   
   <h1>Your Cart</h1>
   <!-- Component code -->
   
   @code {
       private List<Product> cartItems = new();
       
       protected override async Task OnInitializedAsync()
       {
           // Initialize cart
       }
   }
   ```

2. **Add scoped styles** in `Cart.razor.css`:
   ```css
   .cart-container {
       padding: 2rem;
   }
   ```

3. **Add navigation** in `MainLayout.razor`:
   ```razor
   <NavLink href="cart">Cart</NavLink>
   ```

### Database Migrations

When changing the `Product` model:

1. **Update the model** in `DataEntities/Product.cs`

2. **Update EF configuration** in `Products/Data/ProductDataContext.cs`:
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);
       
       modelBuilder.Entity<Product>(entity =>
       {
           // Add new property configuration
           entity.Property(e => e.NewProperty)
               .HasMaxLength(100);
       });
   }
   ```

3. **For LocalDB, drop and recreate**:
   - Delete database in SQL Server Object Explorer
   - Restart application (auto-creates with new schema)

4. **For production, use migrations**:
   ```bash
   dotnet ef migrations add AddNewProperty --project Products
   dotnet ef database update --project Products
   ```

---

## Configuration

### Application Settings

#### Products API (`Products/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "ProductsDb": "Server=(localdb)\\MSSQLLocalDB;Database=TestDB;
                   Integrated Security=true;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### Store UI (`Store/appsettings.json`)

```json
{
  "ProductEndpoint": "https://localhost:7130",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Environment Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development`, `Production` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry endpoint | `http://localhost:4317` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Monitor | `InstrumentationKey=...` |

### CORS Configuration

**Current Policy** (Products API):
```csharp
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

**For Production**, restrict to specific origins:
```csharp
policy.WithOrigins("https://yourdomain.com")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

---

## API Reference

### Products API Endpoints

Base URL: `https://localhost:7130`

#### Get All Products
```http
GET /api/Product
```

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "name": "Solar Powered Flashlight",
    "description": "A fantastic product for outdoor enthusiasts",
    "price": 19.99,
    "imageUrl": null,
    "imageData": null,
    "createdDate": "2025-01-01T00:00:00Z",
    "modifiedDate": "2025-01-01T00:00:00Z"
  }
]
```

#### Get Product by ID
```http
GET /api/Product/{id}
```

**Parameters:**
- `id` (path, integer, required): Product ID

**Responses:**
- `200 OK`: Product found
- `404 Not Found`: Product not found

#### Get Product Image
```http
GET /api/Product/{id}/image
```

**Parameters:**
- `id` (path, integer, required): Product ID

**Responses:**
- `200 OK`: Returns PNG image (`image/png`)
- `404 Not Found`: Product not found or no image data

#### Create Product
```http
POST /api/Product
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "New Product",
  "description": "Product description",
  "price": 29.99,
  "imageUrl": "https://example.com/image.png"
}
```

**Response:** `201 Created`
```json
{
  "id": 10,
  "name": "New Product",
  ...
}
```

**Location Header:** `/api/Product/10`

#### Update Product
```http
PUT /api/Product/{id}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Updated Product",
  "description": "Updated description",
  "price": 34.99,
  "imageUrl": "https://example.com/new-image.png"
}
```

**Responses:**
- `204 No Content`: Update successful
- `404 Not Found`: Product not found

#### Upload Product Image
```http
PUT /api/Product/{id}/image
Content-Type: multipart/form-data
```

**Form Data:**
- `file` (file, required): Image file (PNG, JPG, etc.)

**Responses:**
- `204 No Content`: Upload successful
- `400 Bad Request`: Invalid file
- `404 Not Found`: Product not found

#### Delete Product
```http
DELETE /api/Product/{id}
```

**Responses:**
- `204 No Content`: Delete successful
- `404 Not Found`: Product not found

#### Get Product Count
```http
GET /api/Product/count
```

**Response:** `200 OK`
```json
9
```

#### Debug Images
```http
GET /api/Product/debug/images
```

**Response:** `200 OK`
```json
{
  "totalProducts": 9,
  "productsWithImageUrl": 0,
  "productsWithImageData": 9,
  "products": [
    {
      "id": 1,
      "name": "Solar Powered Flashlight",
      "imageUrl": null,
      "hasImageData": true,
      "imageDataSize": 45678
    }
  ]
}
```

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Errors

**Symptom:**
```
SqlException: A network-related or instance-specific error occurred
```

**Solutions:**
- Verify SQL Server LocalDB is installed
- Check connection string in `appsettings.json`
- Restart SQL Server LocalDB:
  ```bash
  sqllocaldb stop MSSQLLocalDB
  sqllocaldb start MSSQLLocalDB
  ```

#### 2. CORS Errors in Browser

**Symptom:**
```
Access to fetch at 'https://localhost:7130/api/Product' 
from origin 'http://localhost:7085' has been blocked by CORS policy
```

**Solution:**
- Ensure CORS is configured in `Products/Program.cs`
- Verify `app.UseCors("AllowBlazorClient")` is called
- Check CORS policy allows the Store origin

#### 3. Products API Not Starting

**Symptom:**
```
Port 7130 is already in use
```

**Solutions:**
- Stop other instances of the API
- Change port in `Properties/launchSettings.json`
- Use `netstat -ano | findstr :7130` to find process

#### 4. Images Not Displaying

**Symptom:**
- Product cards show broken images
- Network tab shows 404 for image requests

**Solutions:**
- Run `LoadImages.ps1` script from `Products/SQL/` directory
- Verify `ImageData` is populated in database
- Check `ProductService.GetImageUrl()` logic
- Inspect `/api/Product/debug/images` endpoint

#### 5. Tests Failing

**Symptom:**
```
System.InvalidOperationException: Unable to resolve service 
for type 'ProductDataContext'
```

**Solutions:**
- Ensure test project references all required projects
- Check `WebApplicationFactory` configuration
- Verify database seeding in test setup
- Clear `bin/` and `obj/` directories, rebuild

#### 6. Coverage Report Not Generating

**Symptom:**
```
File '...ProductSerializerContext.g.cs' does not exist (any more)
```

**Solution:**
- This is a known warning for source-generated files
- HTML report is still generated successfully
- To suppress: exclude `obj/` directories in ReportGenerator filters
  ```bash
  -filefilters:-*.g.cs;-**/obj/**
  ```

#### 7. Aspire Dashboard Not Opening

**Symptom:**
- Dashboard doesn't launch after starting AppHost
- Browser shows connection refused

**Solutions:**
- Check console output for dashboard URL
- Verify no firewall blocking localhost ports
- Try accessing manually: `http://localhost:15888`
- Check `TinyShop.AppHost/Properties/launchSettings.json`

### Performance Issues

#### Slow First Request

**Cause:** Database initialization and seeding on first run

**Solutions:**
- Expected behavior for development
- For production: pre-create and seed database
- Use in-memory database for fast tests

#### Blazor Page Load Slow

**Cause:** Multiple HTTP requests to fetch all products

**Solutions:**
- Implement pagination in Products API
- Add caching with `IMemoryCache`
- Use lazy loading for product images
- Consider SignalR for real-time updates

### Debug Tips

1. **Enable Detailed Logging:**
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Debug",
       "Microsoft.EntityFrameworkCore": "Information"
     }
   }
   ```

2. **Inspect Aspire Dashboard:**
   - View distributed traces
   - Check service logs
   - Monitor metrics

3. **Use Browser DevTools:**
   - Network tab for API calls
   - Console for JavaScript errors
   - Blazor Server logs in browser console

4. **Check Health Endpoints:**
   ```bash
   curl https://localhost:7130/health
   curl http://localhost:7085/health
   ```

---

## Additional Resources

### Documentation
- [.NET 9 Documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

### Lab Instructions
- Part 1: Code Completion with Ghost Text
- Part 3: Referencing Files in Chat
- Part 4: Using Custom Instructions
- Part 5: Implementing Features with Copilot Edits
- Part 7: Debugging with Copilot

### Related Repositories
- [Official Lab Repository](https://github.com/dotnet-presentations/build-2025-lab300)
- [.NET Aspire Samples](https://github.com/dotnet/aspire-samples)
- [Blazor Samples](https://github.com/dotnet/blazor-samples)

---

## Contributing

When contributing to this solution:

1. Follow .NET coding conventions
2. Write integration tests for new features
3. Maintain code coverage above 60%
4. Update this guide with significant changes
5. Follow the Copilot instructions in `.github/copilot-instructions.md`

### Code Style Guidelines
- Use Minimal API patterns for endpoints
- Follow Blazor component naming conventions
- Place scoped styles in `.razor.css` files
- Use `async`/`await` consistently
- Implement proper error handling

---

**Last Updated:** January 2025  
**Version:** 1.0  
**Maintainer:** TinyShop Team
