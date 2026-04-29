# Products SQL Provisioning

## Overview

This folder contains the SQL provisioning scripts and database init container for TinyShop.

The Docker Compose stack uses `src/Products/SQL/init-db.sh` to configure the SQL Server instance and create `TinyShopDB` before the Products API starts.

## How it works

- `src/Products/SQL/Setup.sql` creates `TinyShopDB`, enables preview features, and provisions the `TinyShopUser` login
- `src/Products/SQL/init-db.sh` runs in the `init-db` container and applies setup logic
- If `TinyShopDB` does not exist, `init-db.sh` imports `TinyShopDB-2026-Initialized.bacpac` for faster startup
- If `TinyShopDB` exists and the `Products` table is empty, `init-db.sh` drops and re-imports the database from the bacpac
- If the database exists with products, the bacpac import is skipped to preserve data

## Startup

Run the Compose stack from the repository root:

```bash
docker compose up -d --build
```

The `init-db` service waits for SQL Server to become healthy and then runs `/usr/src/sql/init/init-db.sh`.

## Manual SQL provisioning

If you need to provision the database manually, use `sqlcmd` against the SQL Server container and rely on the `MSSQL_SA_PASSWORD` environment variable:

```bash
pwsh -NoProfile -Command "sqlcmd -S localhost,1433 -U sa -P $env:MSSQL_SA_PASSWORD -i src/Products/SQL/Setup.sql"
```

Or with a bash shell:

```bash
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -i src/Products/SQL/Setup.sql
```

## Load image data

The helper script `LoadImages.sql` can load PNG images from the mounted `/usr/src/sql/images` folder into `dbo.Products.ImageData`.
This script requires `OPENROWSET` and elevated permissions and is not the recommended production path.

Preferred option:
- use the API endpoint `PUT /api/Product/{id}/image`

## Files

- `Setup.sql` — provisions `TinyShopDB` and `TinyShopUser`
- `init-db.sh` — init container startup script
- `TinyShopDB-2026-Initialized.bacpac` — optional prebuilt database archive
- `LoadImages.sql` — helper script for loading image binaries into the database
