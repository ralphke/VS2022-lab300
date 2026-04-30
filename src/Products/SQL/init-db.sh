#!/usr/bin/env bash
# init-db startup script
set -euo pipefail

SQLCMD="sqlcmd"
PASSWORD="${MSSQL_SA_PASSWORD:-}"
LOG_FILE="/tmp/init-db-embed-errors.log"
SERVER="sqlserver"
DATABASE="TinyShopDB"
SCRIPT_DIR="/usr/src/sql/init"
BACPAC_FILE="$SCRIPT_DIR/TinyShopDB-2026-Initialized.bacpac"
SQLPACKAGE="sqlpackage"

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "sqlcmd not found in PATH. Locating sqlcmd..."
  if [ -x "/opt/mssql-tools/bin/sqlcmd" ]; then
    export PATH="/opt/mssql-tools/bin:$PATH"
    echo "Added /opt/mssql-tools/bin to PATH"
  else
    FOUND_SQLCMD=$(find / -type f -name sqlcmd 2>/dev/null | head -n 1 || true)
    if [ -n "$FOUND_SQLCMD" ] && [ -x "$FOUND_SQLCMD" ]; then
      export PATH="$(dirname "$FOUND_SQLCMD"):$PATH"
      echo "Added $(dirname "$FOUND_SQLCMD") to PATH"
    else
      echo "ERROR: sqlcmd executable not found. Install mssql-tools or verify sqlcmd is available in PATH."
      exit 1
    fi
  fi
fi

if [ -f "$BACPAC_FILE" ] && ! command -v "$SQLPACKAGE" >/dev/null 2>&1; then
  echo "sqlpackage not found in PATH. Locating sqlpackage..."
  if [ -x "/root/.dotnet/tools/sqlpackage" ]; then
    SQLPACKAGE="/root/.dotnet/tools/sqlpackage"
    export PATH="$(dirname "$SQLPACKAGE"):$PATH"
    echo "Added $(dirname "$SQLPACKAGE") to PATH"
  else
    FOUND_SQLPACKAGE=$(find / -type f -name sqlpackage 2>/dev/null | head -n 1 || true)
    if [ -n "$FOUND_SQLPACKAGE" ] && [ -x "$FOUND_SQLPACKAGE" ]; then
      SQLPACKAGE="$FOUND_SQLPACKAGE"
      export PATH="$(dirname "$SQLPACKAGE"):$PATH"
      echo "Added $(dirname "$SQLPACKAGE") to PATH"
    fi
  fi
fi

if [ -z "$PASSWORD" ]; then
  echo "MSSQL_SA_PASSWORD is not set"
  exit 1
fi

echo "Waiting for SQL Server to be ready (with extended timeout for sa user initialization)..."
start_time=$(date +%s)
max_wait=300
connection_attempts=0

while true; do
  connection_attempts=$((connection_attempts + 1))
  
  # Try to connect and run a simple query
  if $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; then
    echo "SQL Server is ready and sa user is accessible (attempt $connection_attempts)."
    break
  fi

  now=$(date +%s)
  elapsed=$((now - start_time))
  
  if [ $elapsed -ge $max_wait ]; then
    echo "ERROR: Timed out waiting for SQL Server after $max_wait seconds"
    echo "Last connection attempt: $connection_attempts"
    $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -Q "SELECT 1" 2>&1 || true
    exit 1
  fi

  if [ $((connection_attempts % 5)) -eq 0 ]; then
    echo "Waiting for SQL Server... (attempt $connection_attempts, elapsed ${elapsed}s)"
  fi
  sleep 2
done

echo "Clearing previous embedding error log: $LOG_FILE"
rm -f "$LOG_FILE"

# ── Database Initialization ──────────────────────────────────────────────────
echo "Running database setup scripts..."

# Always run Setup.sql first because it configures the SQL Server environment even when a bacpac is available.
TINYSHOP_USER_PASSWORD="${TinyShopUser_PASSWORD:-$PASSWORD}"

if [ -f "$SCRIPT_DIR/Setup.sql" ]; then
  echo "Running Setup.sql..."
  if ! $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -v "TinyShopUserPassword=$TINYSHOP_USER_PASSWORD" -i "$SCRIPT_DIR/Setup.sql" 2>&1 | tee -a "$LOG_FILE"; then
    echo "Warning: Setup.sql had errors (may be non-critical)"
  fi
else
  echo "Warning: Setup.sql not found at $SCRIPT_DIR/Setup.sql"
fi

# Check if database exists - use simple query without grep to avoid character issues
echo "Checking if database $DATABASE exists..."
DB_CHECK=$($SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = '$DATABASE';" 2>&1 || echo "ERROR")

if [[ "$DB_CHECK" == "ERROR" ]] || [[ -z "$DB_CHECK" ]]; then
  echo "ERROR: Could not connect to SQL Server to check database existence"
  echo "Connection check output: $DB_CHECK"
  exit 1
fi

# Trim whitespace
DB_CHECK=$(echo "$DB_CHECK" | xargs)

if [ "$DB_CHECK" -eq 0 ]; then
  if [ -f "$BACPAC_FILE" ]; then
    echo "Database $DATABASE does not exist. Importing bacpac via sqlpackage."
    if ! command -v "$SQLPACKAGE" >/dev/null 2>&1; then
      echo "ERROR: sqlpackage is required to import bacpac but could not be found."
      exit 1
    fi

    if ! "$SQLPACKAGE" /Action:Import /SourceFile:"$BACPAC_FILE" /TargetServerName:"$SERVER" /TargetDatabaseName:"$DATABASE" /TargetUser:"sa" /TargetPassword:"$PASSWORD" /TargetEncryptConnection:Optional /TargetTrustServerCertificate:True /Quiet 2>&1 | tee /tmp/init-db-import.log; then
      echo "ERROR: Failed to import database from bacpac. See /tmp/init-db-import.log for details."
      exit 1
    fi

    echo "Database imported successfully from bacpac."
    exit 0
  fi

  echo "Database $DATABASE does not exist and no bacpac is available. Initialization complete."
  exit 0
fi

if [ -f "$BACPAC_FILE" ]; then
  echo "Database $DATABASE exists. Checking whether the Products table is empty."
  PROD_COUNT=$($SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -d "$DATABASE" -h -1 -W -Q "SET NOCOUNT ON; IF OBJECT_ID(N'dbo.Products','U') IS NOT NULL SELECT COUNT(*) FROM dbo.Products ELSE SELECT 0;" 2>&1 || echo "ERROR")

  if [[ "$PROD_COUNT" == "ERROR" ]] || [[ -z "$PROD_COUNT" ]]; then
    echo "ERROR: Could not query Products table row count"
    echo "Query output: $PROD_COUNT"
    exit 1
  fi

  PROD_COUNT=$(echo "$PROD_COUNT" | xargs)
  if [ "$PROD_COUNT" -eq 0 ]; then
    echo "Products table is empty. Overwriting database $DATABASE from bacpac."
    if ! $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -Q "ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DATABASE];" 2>&1 | tee -a "$LOG_FILE"; then
      echo "ERROR: Failed to drop existing database $DATABASE."
      exit 1
    fi

    if ! "$SQLPACKAGE" /Action:Import /SourceFile:"$BACPAC_FILE" /TargetServerName:"$SERVER" /TargetDatabaseName:"$DATABASE" /TargetUser:"sa" /TargetPassword:"$PASSWORD" /TargetEncryptConnection:Optional /TargetTrustServerCertificate:True /Quiet 2>&1 | tee /tmp/init-db-import.log; then
      echo "ERROR: Failed to import database from bacpac. See /tmp/init-db-import.log for details."
      exit 1
    fi

    echo "Database imported successfully from bacpac."
    exit 0
  fi

  echo "Products table contains data. Skipping bacpac import."
fi

echo "Database $DATABASE already exists and is populated. Initialization complete."

echo ""
echo "Database initialization complete!"
echo "Log file: $LOG_FILE"
if [ -f "$LOG_FILE" ] && [ -s "$LOG_FILE" ]; then
  echo "Errors/warnings logged to: $LOG_FILE"
  echo "---"
  cat "$LOG_FILE" || echo "Could not read log file"
fi
