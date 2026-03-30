## TinyShop

TinyShop is a .NET Aspire cloud-native e-commerce app. The Aspire AppHost (`TinyShop.AppHost`) orchestrates all services including a SQL Server container, the Products API, and the Store Blazor frontend.

## Dev Container

A devcontainer is configured in `.devcontainer/`. It provides a ready-to-code Linux environment with .NET 10, Docker-in-Docker (required for Aspire to run the SQL Server container), Node.js, the Aspire CLI, and the C# DevKit VS Code extension — no local tooling needed.

Open in GitHub Codespaces or VS Code with the Dev Containers extension, then:

```bash
aspire run          # starts sqlserver → products → store
```

Forwarded ports (HTTP):
- `15218` — Aspire Dashboard (opens automatically)
- `5228`  — Products API
- `5158`  — Store (Blazor)

## Running and Testing

```bash
# Run the full application (preferred — starts all services + SQL Server container)
aspire run

# Run all tests
dotnet test src/TinyShop.sln

# Run a single test project
dotnet test src/Store.Tests

# Run a single test by name
dotnet test src/Store.Tests --filter "FullyQualifiedName~AddItem_WhenProductIsNew"

# Run benchmarks (always use Release)
dotnet run --project src/BenchmarkSuite1 --configuration Release
```

> Restart with `aspire run` only when `TinyShop.AppHost/Program.cs` changes. Hot reload handles all other changes.

## Architecture

```
TinyShop.AppHost        → Aspire orchestrator; defines all resources and wiring
DataEntities            → Shared Product model (used by Products API and Store)
Products                → ASP.NET Core Minimal API; EF Core + SQL Server; port 7130
Store                   → Blazor Server (Interactive Server Components); port 7085
TinyShop.ServiceDefaults→ Shared OpenTelemetry, health checks, resilience config
Store.Tests             → xUnit unit/component tests (FluentAssertions, bUnit, Moq)
Tests/IntegrationTests  → xUnit API + UI integration tests (WebApplicationFactory)
Tests/TinyShopTest      → MSTest basic tests
BenchmarkSuite1         → BenchmarkDotNet performance benchmarks
```

**Startup order** (enforced by Aspire): `sqlserver` → `products` → `store`

**Service discovery**: Store references the Products API via service name `"https+http://products"` (resolved by Aspire). The `ProductEndpoint` config key holds this value.

## Backend — Products API

All routes are under `/api/Product`. New endpoints go in `Products/Endpoints/ProductEndpoints.cs` using the `MapGroup` pattern with `.WithName()` and `.Produces<T>()` on every endpoint:

```csharp
group.MapGet("/{id:int}", handler)
    .WithName("GetProductById")
    .Produces<Product>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);
```

The database is SQL Server (not in-memory). `InitializeDatabaseAsync()` runs at startup: it calls `EnsureCreatedAsync()` then seeds 9 products and loads their images from `wwwroot/images/` into the `ImageData` (varbinary) column if the table is empty.

## Database Seeding and Image Loading

**Automatic (via Aspire — preferred)**: On first `aspire run`, `DbInitializer.InitializeAsync()` seeds the 9 products then immediately calls `LoadImagesAsync()`, which reads `Products/wwwroot/images/product1.png` … `product9.png` and writes the bytes into the `ImageData` column. This is idempotent — it only runs when the `Products` table is empty.

**If the DB already exists but images are missing** (e.g., after a partial setup or schema reset), use the API upload endpoint:

```bash
# Upload an image for product ID 1 (repeat for each product)
curl -X PUT "https://localhost:7130/api/Product/1/image" \
  -F "file=@src/Products/wwwroot/images/product1.png"
```

**Manual LocalDB setup** (without Aspire container):
```bash
# Create database, table, user, and seed rows
sqlcmd -S "(localdb)\MSSQLLocalDB" -i src/Products/SQL/Setup.sql
```
`LoadImages.sql` uses `OPENROWSET(BULK ...)` which requires `Ad Hoc Distributed Queries` to be enabled — use the API upload approach above instead.

**Verify image state** at any time via the debug endpoint:
```
GET /api/Product/debug/images
```
Returns per-product `HasImageData` flag and `ImageDataSize` so you can see which rows are populated without querying the DB directly.

**Image serving flow end-to-end**:
```
Store Blazor page
  → ProductService.GetImageUrl(product)   // returns "/api/images/{id}"
  → Store bridge  GET /api/images/{id}    // Program.cs in Store
  → ProductService.GetProductImageAsync() // cached 30 min in IMemoryCache
  → Products API  GET /api/Product/{id}/image  // returns raw PNG bytes
  → ImageData column in SQL Server
```

The `ImageUrl` column (`images/product1.png`) is a legacy fallback for static file serving and is only used when `product.Id == 0` (i.e., the product has never been saved).

## Frontend — Blazor Store

**ProductService** (`Store/Services/ProductService.cs`) is the only HTTP client for the Products API. It is registered as a typed `HttpClient`. Product list results are cached for 5 minutes; image bytes for 30 minutes.

**CartService** (`Store/Services/CartService.cs`) is registered as `AddScoped` — each user session has its own cart. It exposes `event Action OnChange` for UI reactivity.

**Image display**: Images are served by the Store's own bridge endpoint `/api/images/{id}`, which fetches bytes from the Products API. Use `ProductService.GetImageUrl(product)` to get the correct URL — never construct image paths manually. When displaying images with `ImagePrefix` from configuration, inject it via `@inject IConfiguration Configuration` and access `Configuration["ImagePrefix"]`.

## Shared Model — DataEntities

`Product.ImageData` (byte[]) is `[JsonIgnore]` — it is never sent over the wire directly. The `ImageDataBase64` computed property handles base64 serialization when needed. Use `ProductSerializerContext.Default.ListProduct` for source-generated JSON deserialization of product lists.

## Conventions

- **CSS**: All component styles go in a `.razor.css` file alongside the `.razor` file. No inline styles.
- **Responsive grid**: Use `repeat(auto-fit, minmax(280px, 1fr))` with breakpoints at 1400/992/768/576/400px.
- **Tests**: Use FluentAssertions (`.Should().Be()`) not Assert methods. Test class names follow `{Subject}Tests`.
- **Aspire resources**: Before adding a new integration, use the *list integrations* MCP tool to find the version matching `Aspire.AppHost.Sdk` (currently `13.2.0` per `src/global.json`).
- **Ignore**: All `*.md` files in the `lab/` folder — those are lab instructions, not production code guidance.
