# TinyShop Database Initialization

## Overview

This repository uses a containerized SQL Server workflow with a dedicated `init-db` service for database provisioning.

The database initialization process is split into two phases:

1. `src/Products/SQL/Setup.sql` provisions the database and SQL login/user
2. `src/Products/SQL/init-db.sh` runs inside the init container to apply setup and optionally import the shipped bacpac

The Products API also performs runtime maintenance on startup to ensure schema, seed initial products, load images, and build embeddings.

## Start the stack

From the repository root:

```bash
docker compose up -d --build
```

The compose stack includes:

- `sqlserver` — SQL Server container
- `init-db` — database provisioning container
- `embeddings` — local Hugging Face embeddings service
- `products` — Products API
- `store` — Blazor Store UI

## How initialization works

- `sqlserver` starts first and exposes port `1433`
- `init-db` waits for the SQL Server container to become healthy
- `init-db` runs `src/Products/SQL/init-db.sh`
- `init-db.sh` executes `Setup.sql` and then may import the shipped bacpac if the database is absent or the `Products` table is empty
- `products` starts after `init-db` completes successfully
- `Products/Program.cs` then performs application-level database maintenance on startup

## Bacpac behavior

The file `src/Products/SQL/TinyShopDB-2026-Initialized.bacpac` is available if the database needs a fast restore.

`init-db.sh` uses the bacpac to:

- import the database when `TinyShopDB` does not exist
- overwrite the database when `TinyShopDB` exists but `dbo.Products` is empty
- skip import when `TinyShopDB` already exists and contains products

## Manual reset

To reset the database, stop the stack and remove the SQL data volume:

```bash
docker compose down -v
```

Then start again:

```bash
docker compose up -d --build
```

## Files

- `src/Products/SQL/Setup.sql` — SQL provisioning for `TinyShopDB` and `TinyShopUser`
- `src/Products/SQL/init-db.sh` — init container startup script
- `src/Products/SQL/TinyShopDB-2026-Initialized.bacpac` — optional prebuilt database archive
- `src/Products/SQL/LoadImages.sql` — helper script for loading product image bytes from disk

## Environment variables

The SQL Compose stack reads `MSSQL_SA_PASSWORD` from the repository root `.env` file.

Example:

```env
MSSQL_SA_PASSWORD=your-sa-password-here
```

## Troubleshooting

- If `init-db` fails, inspect logs:

```bash
docker compose logs init-db
```

- If Docker access fails inside the devcontainer, rebuild/reopen the container after the post-create setup runs.
