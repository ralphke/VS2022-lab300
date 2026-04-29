<p align="center">
<img src="img/banner.jpg" alt="decorative banner" width="1200"/>
</p>

# TinyShop-Agentic - Hands-on with GitHub Copilot in Visual Studio 2022

This lab workspace contains a containerized .NET 10 e-commerce sample that runs with Aspire, Docker Compose, and a local devcontainer.

## Prerequisites

- .NET 10 SDK installed if running the app from the host
- Docker Desktop or another Docker engine with `docker compose` available
- GitHub Copilot extension for Visual Studio / VS Code if using the lab exercises
- Recommended: add your user to the `docker` group when running Docker locally
- If running the repo in the devcontainer, Docker Desktop must be configured to expose the host socket

## Current Workspace State

- The repository contains a local Docker Compose stack for `sqlserver`, `init-db`, `embeddings`, `products`, and `store`
- `src/Products/SQL/init-db.sh` provisions `TinyShopDB` and can import `TinyShopDB-2026-Initialized.bacpac`
- `src/Products/SQL/Setup.sql` creates `TinyShopDB`, enables SQL preview features, and provisions `TinyShopUser`
- The `Products` API performs runtime maintenance on startup: schema checks, seeding, image loading, embeddings, and vector index creation
- The `Store` frontend uses a typed `ProductService` HTTP client and reads `ProductEndpoint` from configuration
- The devcontainer installs Aspire, SQL tooling, and configures Docker socket access

## Run with Docker Compose

The stack is started with Docker Compose:

```bash
docker compose up -d --build
```

The `init-db` service runs after SQL Server is healthy and applies provisioning scripts before the `products` service starts.

Stop the stack:

```bash
docker compose down
```

### Environment file

The Compose stack reads `MSSQL_SA_PASSWORD` from `.env` at the repository root.

Example `.env`:

```env
MSSQL_SA_PASSWORD=your-sa-password-here
```

### Useful endpoints

- Store: http://localhost:5158
- Products API: http://localhost:5228/api/Product

### Health check

```bash
curl -s -o /dev/null -w "products:%{http_code}\n" http://localhost:5228/api/Product
curl -s -o /dev/null -w "store:%{http_code}\n" http://localhost:5158/
```

## Run with Aspire

If you want to run the app as a .NET Aspire orchestrated solution instead of Docker Compose, use:

```bash
aspire run
```

Aspire will start the SQL server, Products API, and Store UI services according to the app host configuration.

## Run Tests

Run all test projects from the solution:

```bash
dotnet test src/TinyShop.sln
```

Or run individual projects:

```bash
dotnet test src/Store.Tests/Store.Tests.csproj
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
dotnet test src/Tests/TinyShopTest/TinyShopTest.csproj
```

## Dev Container

This repository includes a Linux devcontainer in `.devcontainer/devcontainer.json`.
The container mounts the host Docker socket and installs Aspire and SQL tooling in `postCreate.sh`.

Use `Dev Containers: Reopen in Container` from VS Code, then run:

```bash
aspire run
```

If Docker access fails inside the container, rebuild/reopen after the post-create step adds the container user to the `docker` group.

## Notes

- The init-db service is responsible for database provisioning in Docker Compose setups
- The `Products` service still performs schema maintenance and seeding on startup
- Product images are stored as binary `ImageData` and served by the Products API image endpoint
