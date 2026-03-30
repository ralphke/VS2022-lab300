create database TestDB;
go
use TestDB;
go

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
    DROP TABLE dbo.Products;
GO

CREATE TABLE [dbo].[Products] (
    [Id]           INT             IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (200)  NOT NULL,
    [Description]  NVARCHAR (1000) NULL,
    [Price]        DECIMAL (18, 2) NOT NULL,
    [ImageUrl]     NVARCHAR (500)  NULL,
    [ImageData]    VARBINARY (MAX) NULL,
    [CreatedDate]  DATETIME2 (7)   DEFAULT (getutcdate()) NULL,
    [ModifiedDate] DATETIME2 (7)   DEFAULT (getutcdate()) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Products_Name]
    ON [dbo].[Products]([Name] ASC);


BEGIN TRANSACTION;

INSERT INTO Products (name, Description, Price, ImageUrl, ImageData) VALUES
('Solar Powered Flashlight', 'A fantastic product for outdoor enthusiasts', 19.99, 'images/product1.png', NULL),
('Hiking Poles', 'Ideal for camping and hiking trips', 24.99, 'images/product2.png', NULL),
('Outdoor Rain Jacket', 'This product will keep you warm and dry in all weathers', 49.99, 'images/product3.png', NULL),
('Survival Kit', 'A must-have for any outdoor adventurer', 99.99, 'images/product4.png', NULL),
('Outdoor Backpack', 'This backpack is perfect for carrying all your outdoor essentials', 39.99, 'images/product5.png', NULL),
('Camping Cookware', 'This cookware set is ideal for cooking outdoors', 29.99, 'images/product6.png', NULL),
('Camping Stove', 'This stove is perfect for cooking outdoors', 49.99, 'images/product7.png', NULL),
('Camping Lantern', 'This lantern is perfect for lighting up your campsite', 19.99, 'images/product8.png', NULL),
('Camping Tent', 'This tent is perfect for camping trips', 99.99, 'images/product9.png', NULL);

COMMIT;
GO

-- Attempt to enable Ad Hoc Distributed Queries (requires sysadmin)
BEGIN TRY
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE WITH OVERRIDE;
    EXEC sp_configure 'Ad Hoc Distributed Queries', 1;
    RECONFIGURE WITH OVERRIDE;
END TRY
BEGIN CATCH
    -- If enabling fails, continue - the OPENROWSET calls may still work if already enabled.
    PRINT 'Warning: Could not enable Ad Hoc Distributed Queries in this session or insufficient permissions.';
    PRINT ERROR_MESSAGE();
END CATCH
GO

-- Base folder where images are stored on the SQL Server machine.
DECLARE @BasePath nvarchar(4000) = N'D:\repros\VS2022-lab300\src\Products\wwwroot\images\product';


-- Use dynamic SQL per-row to pass a literal path into OPENROWSET(BULK ...)
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @ProductId INT;
    DECLARE @ImageUrl nvarchar(255);
    DECLARE @ImagePath nvarchar(4000);
    DECLARE @sql nvarchar(max);
    DECLARE @ImageIndex INT = 1;
    DECLARE @MaxImages INT = 9;

    -- Cursor to iterate through products ordered by ID
    DECLARE product_cursor CURSOR FOR
        SELECT Id FROM dbo.Products ORDER BY Id;

    OPEN product_cursor;
    FETCH NEXT FROM product_cursor INTO @ProductId;

    WHILE @@FETCH_STATUS = 0 AND @ImageIndex <= @MaxImages
    BEGIN
        -- Construct the image file path based on sequential index
        SET @ImagePath = @BasePath  + CAST(@ImageIndex AS NVARCHAR(10)) + N'.png';
    
        -- Build dynamic SQL to load the image
        SET @SQL = N'UPDATE dbo.Products 
                SET ImageData = (SELECT * FROM OPENROWSET(BULK ''' + @ImagePath + N''', SINGLE_BLOB) AS ImageData),
                    ImageUrl = ''images/product' + CAST(@ImageIndex AS NVARCHAR(10)) + N'.png''
         WHERE Id = ' + CAST(@ProductId AS NVARCHAR(10)) + N';';
    
        -- Execute the dynamic SQL
      BEGIN TRY
            EXEC sp_executesql @SQL;
            PRINT 'Successfully loaded image ' + CAST(@ImageIndex AS NVARCHAR(10)) + ' for Product ID: ' + CAST(@ProductId AS NVARCHAR(10)) + ' - ' + @ImagePath;
        END TRY
        BEGIN CATCH
          PRINT 'Error loading image ' + CAST(@ImageIndex AS NVARCHAR(10)) + ' for Product ID: ' + CAST(@ProductId AS NVARCHAR(10)) + ' - ' + ERROR_MESSAGE();
      END CATCH
    
        -- Move to next product and image
        SET @ImageIndex = @ImageIndex + 1;
        FETCH NEXT FROM product_cursor INTO @ProductId;
    END

    CLOSE product_cursor;
    DEALLOCATE product_cursor;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    PRINT 'Error during image load: ' + ERROR_MESSAGE();
    ROLLBACK TRANSACTION;
END CATCH
GO

-- If OPENROWSET with dynamic path is not allowed in your environment, uncomment and use static explicit updates:
-- UPDATE Products
-- SET image = (SELECT BulkColumn FROM OPENROWSET(BULK N'D:\repros\VS2022-lab300\src\Products\wwwroot\images\product1.png', SINGLE_BLOB) AS img)
-- WHERE ImageUrl = 'product1.png';

-- Show results
select Id, name, ImageUrl, DATALENGTH([image]) as ImageBytes from Products;
GO
