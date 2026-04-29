# TinyShop Sample Workspace

## Overview

This repository contains a .NET 10 Products API and Blazor Store frontend for a sample e-commerce app.

### Key runtime components

- `Products` — ASP.NET Core Minimal API with SQL Server and semantic search
- `Store` — Blazor Server frontend
- `TinyShop.AppHost` — Aspire orchestrator for local workflows
- `src/Products/SQL` — database provisioning scripts and init container logic

## Run Options

### Option A: Aspire (recommended)

```bash
aspire run
```

Aspire starts the local development services, including SQL Server, Products, and Store.

### Option B: Docker Compose

From the repository root:

```bash
docker compose up -d --build
```

Stop the stack:

```bash
docker compose down
```

## Database Initialization

The Docker Compose workflow uses the `init-db` service to provision the database via `src/Products/SQL/init-db.sh` and `src/Products/SQL/Setup.sql`.

The Products app also checks the database on startup and will seed initial products, load images, and create embeddings when needed.

## Image Loading

If image bytes are missing for existing products, use the API endpoint:

```bash
curl -X PUT "http://localhost:5228/api/Product/1/image"   -F "file=@src/Products/wwwroot/images/product1.png"
```

You can also inspect image state with:

```bash
curl http://localhost:5228/api/Product/debug/images
```

## Tests

Run all tests with:

```bash
dotnet test src/TinyShop.sln
```
