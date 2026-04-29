-- SQL Server Setup Script for TinyShopDB
--
--   PowerShell against SQL container:
--     $pw = (Get-Content .env | Select-String 'MSSQL_SA_PASSWORD').ToString().Split('=',2)[1]
--     sqlcmd -S localhost,1433 -U sa -P "$pw" -i src/Products/SQL/Setup.sql
--
--   cmd against SQL container:
--     for /f "tokens=2 delims==" %i in ('findstr MSSQL_SA_PASSWORD .env') do set _PW=%i
--     sqlcmd -S localhost,1433 -U sa -P "%_PW%" -i src/Products/SQL/Setup.sql
--
-- The MSSQL_SA_PASSWORD value is defined in the .env file at the repository root.
--
-- This script creates TinyShopDB and provisioning objects only.
-- Application schema creation and product seeding are managed by the app.

SET NOCOUNT ON;
USE MASTER;
GO
-- Enable the external REST endpoint feature for testing embedding service integration
exec sp_configure 'show advanced options', 1;
RECONFIGURE;
exec sp_configure 'external rest endpoint enabled',1;
RECONFIGURE;
exec sp_configure 'show advanced options', 0;
RECONFIGURE;
GO
PRINT 'external rest endpoint support enabled successfully';
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TinyShopDB')
BEGIN
    CREATE DATABASE TinyShopDB COLLATE Latin1_General_100_BIN2_UTF8;
END
GO

USE TinyShopDB;
GO

-- Enable preview features for current database
ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;

-- Verify compatibility level (must be 170+)
IF (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) < 170
BEGIN
    RAISERROR('Database compatibility level must be 170 or higher for VECTOR support', 16, 1);
    RETURN;
END
GO

IF NOT EXISTS (SELECT name
FROM sys.server_principals
WHERE name = 'TinyShopUser')
BEGIN
    CREATE LOGIN TinyShopUser WITH PASSWORD = '$(TinyShopUserPassword)', CHECK_POLICY = OFF;
END
GO

IF NOT EXISTS (SELECT name
FROM sys.database_principals
WHERE name = 'TinyShopUser')
BEGIN
    CREATE USER TinyShopUser FOR LOGIN TinyShopUser;
END
GO

ALTER ROLE db_datareader ADD MEMBER TinyShopUser;
ALTER ROLE db_datawriter ADD MEMBER TinyShopUser;
ALTER ROLE db_ddladmin ADD MEMBER TinyShopUser;
GO

GRANT EXECUTE ON SCHEMA::dbo TO TinyShopUser;
GO

PRINT 'TinyShopDB provisioning complete. Run the application after the database is created.';
GO
