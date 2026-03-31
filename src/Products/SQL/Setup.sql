-- SQL Server LocalDB Setup Script for TinyShop
-- Run this script via sqlcmd in SQLCMD mode, passing the password from .env:
--
--   PowerShell:
--     $pw = (Get-Content .env | Select-String 'MSSQL_SA_PASSWORD').ToString().Split('=',2)[1]
--     sqlcmd -S "(localdb)\MSSQLLocalDB" -i src\Products\SQL\Setup.sql -v MSSQL_SA_PASSWORD="$pw"
--
--   cmd:
--     for /f "tokens=2 delims==" %i in ('findstr MSSQL_SA_PASSWORD .env') do set _PW=%i
--     sqlcmd -S (localdb)\MSSQLLocalDB -i src\Products\SQL\Setup.sql -v MSSQL_SA_PASSWORD="%_PW%"
--
-- The MSSQL_SA_PASSWORD value is defined in the .env file at the repository root.

-- 1. Create the database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TineShopDB')
BEGIN
    CREATE DATABASE TineShopDB;
END
GO

USE TineShopDB;
GO

-- 2. Create SQL Server login and user for the application
-- Note: SQL Server Authentication must be enabled in LocalDB
-- For LocalDB, it's recommended to use Windows Authentication instead
-- If you need SQL Auth, you'll need to enable it via SQL Server Configuration Manager

-- Create a login (SQL Server Authentication)
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'TinyShopUser')
BEGIN
    CREATE LOGIN TinyShopUser WITH PASSWORD = '$(MSSQL_SA_PASSWORD)', CHECK_POLICY = OFF;
END
GO

-- Create database user mapped to the login
USE TineShopDB;
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'TinyShopUser')
BEGIN
    CREATE USER TinyShopUser FOR LOGIN TinyShopUser;
END
GO

-- Grant permissions to the user
ALTER ROLE db_datareader ADD MEMBER TinyShopUser;
ALTER ROLE db_datawriter ADD MEMBER TinyShopUser;
GO

-- 3. Create the Products table with binary image storage
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Products (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Price DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,  -- Keep for backward compatibility
        ImageData VARBINARY(MAX) NULL, -- Binary PNG image data
   CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- 4. Create an index on Name for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Name' AND object_id = OBJECT_ID('dbo.Products'))
BEGIN
    CREATE INDEX IX_Products_Name ON dbo.Products(Name);
END
GO

-- 5. Insert sample data (without images initially)
-- Images can be loaded from files using OPENROWSET or application code
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (Name, Description, Price, ImageUrl)
    VALUES 
     ('Solar Powered Flashlight', 'A fantastic product for outdoor enthusiasts', 19.99, 'product1.png'),
     ('Hiking Poles', 'Ideal for camping and hiking trips', 24.99, 'product2.png'),
        ('Outdoor Rain Jacket', 'This product will keep you warm and dry in all weathers', 49.99, 'product3.png'),
        ('Survival Kit', 'A must-have for any outdoor adventurer', 99.99, 'product4.png'),
   ('Outdoor Backpack', 'This backpack is perfect for carrying all your outdoor essentials', 39.99, 'product5.png'),
        ('Camping Cookware', 'This cookware set is ideal for cooking outdoors', 29.99, 'product6.png'),
      ('Camping Stove', 'This stove is perfect for cooking outdoors', 49.99, 'product7.png'),
        ('Camping Lantern', 'This lantern is perfect for lighting up your campsite', 19.99, 'product8.png'),
        ('Camping Tent', 'This tent is perfect for camping trips', 99.99, 'product9.png');
END
GO

-- 6. Optional: Stored procedure to load images from files
-- Note: This requires xp_cmdshell to be enabled (security consideration)
-- Alternatively, load images via application code

PRINT 'Database setup completed successfully.';
PRINT 'Connection String: Server=(localdb)\MSSQLLocalDB;Database=TineShopDB;User Id=TinyShopUser;Password=<MSSQL_SA_PASSWORD from .env>;TrustServerCertificate=True;';
GO
