# TinyShop - Setup Guide

## Overview

TinyShop is a .NET 9 e-commerce application with:
- **Products API**: Backend API serving product data and images
- **Store**: Blazor Server frontend for product browsing

## Quick Start

### 1. Run the Application

```bash
# Set TinyShop.AppHost as startup project in Visual Studio
# Press F5
```

The application will start with .NET Aspire orchestration.

### 2. Load Product Images

Images are stored in the database. To load them:

```powershell
cd D:\repros\VS2022-lab300\src\Products\SQL
.\LoadImages.ps1
```

This uploads images from `Products/wwwroot/images/` into the database.

### 3. Access the Application

- **Store**: `https://localhost:7085`
- **Products API**: `https://localhost:7130/api/Product`
- **Debug View**: `https://localhost:7085/products/debug`

## Architecture

### Scenario 2: Database Image Serving

Images are served from the database via API endpoints:

```
Database (ImageData column)
    ?
GET /api/Product/{id}/image
    ?
Binary PNG returned to browser
```

### Key Components

**Products API**:
- SQL Server database (LocalDB)
- Entity Framework Core
- Minimal APIs
- CORS enabled for Blazor frontend

**Store (Blazor Server)**:
- Interactive Server rendering
- ProductService for API communication
- Responsive grid layout
- Bootstrap styling

## Configuration

### Store (`Store/appsettings.json`)

```json
{
  "ProductEndpoint": "https+http://products",    // Server-to-server
  "ProductBrowserEndpoint": "https://localhost:7130"  // Browser-accessible URLs
}
```

- `ProductEndpoint`: Used for API calls (service discovery)
- `ProductBrowserEndpoint`: Used for image URLs in HTML

### Products (`Products/Program.cs`)

- CORS enabled for cross-origin image requests
- SQL Server LocalDB connection
- Static files middleware for fallback images

## Database

### Connection String

```
Server=(localdb)\MSSQLLocalDB;Database=TestDB;Integrated Security=true;TrustServerCertificate=True;
```

### Schema

**Products Table**:
- `Id` (int, PK)
- `Name` (nvarchar)
- `Description` (nvarchar)
- `Price` (decimal)
- `ImageUrl` (nvarchar) - NULL in Scenario 2
- `ImageData` (varbinary(max)) - Binary PNG data
- `CreatedDate` (datetime2)
- `ModifiedDate` (datetime2)

## Image Loading

### PowerShell Script (Recommended)

```powershell
.\Products\SQL\LoadImages.ps1
```

Uploads all images from `Products/wwwroot/images/` to database via API.

### SQL Script (Requires Permissions)

```sql
-- Enable Ad Hoc Distributed Queries first
.\Products\SQL\LoadImages.sql
```

Uses OPENROWSET to load images directly into database.

## Troubleshooting

### Images Not Displaying

1. **Check images are loaded**:
 - Visit: `/api/Product/debug/images`
   - Should show: `productsWithImageData: 9`

2. **Verify CORS**:
 - Open browser DevTools (F12)
   - Check Console for CORS errors
   - Should have no errors

3. **Check image endpoint**:
   - Visit: `https://localhost:7130/api/Product/1/image`
   - Should display image directly

4. **Re-run LoadImages.ps1**:
 - Ensure Products API is running
   - Run the script again

### Connection Issues

- Ensure LocalDB is installed
- Check connection string in Products/Program.cs
- Verify database exists (auto-created on first run)

## Development

### Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project TinyShop.AppHost
```

### Database Reset

```sql
USE master;
DROP DATABASE TestDB;
```

Application will recreate and seed on next run.

## Production Considerations

### CORS Policy

Current: `AllowAnyOrigin()` (development)

For production, restrict to specific origins:

```csharp
policy.WithOrigins("https://your-store.com")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

### Image Storage

**Current (Scenario 2)**: Database storage
- Pros: Single source of truth, easy backup
- Cons: Larger database, slower than static files

**Alternative (Scenario 3)**: Static files in `wwwroot/images`
- Pros: Faster, CDN-friendly
- Cons: Separate deployment, no database integration

## Key Features

- ? Product catalog with images
- ? Database image serving
- ? Responsive grid layout
- ? Cross-origin resource sharing (CORS)
- ? .NET Aspire orchestration
- ? Entity Framework Core
- ? Blazor Server interactive components

## Project Structure

```
src/
??? TinyShop.AppHost/         # .NET Aspire orchestration
??? Products/        # Backend API
?   ??? Endpoints/          # API endpoints
?   ??? Data/            # EF Core context
?   ??? SQL/  # Database scripts
?   ??? wwwroot/images/     # Source images
??? Store/        # Blazor frontend
?   ??? Components/Pages/     # Razor pages
?   ??? Services/       # API services
?   ??? wwwroot/   # Static assets
??? DataEntities/         # Shared models
??? TinyShop.ServiceDefaults/ # Common services

```

## Support

For issues or questions, check:
- Products API debug endpoint: `/api/Product/debug/images`
- Store debug page: `/products/debug`
- Browser DevTools Console (F12)

---

**Application ready to run!** ??
