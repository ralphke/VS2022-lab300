-- Helper Script: Load Images from File System into Database
-- This script demonstrates how to load PNG images from disk into the ImageData column
-- NOTE: This requires OPENROWSET which needs special permissions and may not work in all environments
-- For production, use the API endpoint PUT /api/Product/{id}/image instead

USE TestDB;
GO

-- Enable advanced options (requires sysadmin or appropriate permissions)
-- EXEC sp_configURE 'show advanced options', 1;
-- RECONFIGURE;
-- EXEC sp_configURE 'Ad Hoc Distributed Queries', 1;
-- RECONFIGURE;
-- GO

-- Load images for the first 9 products in sequential order by ID
DECLARE @ProductId INT;
DECLARE @ImageIndex INT = 1;
DECLARE @MaxImages INT = 9;
DECLARE @ImagePath NVARCHAR(500);
DECLARE @SQL NVARCHAR(MAX);

-- Cursor to iterate through products ordered by ID
DECLARE product_cursor CURSOR FOR
    SELECT Id FROM dbo.Products ORDER BY Id;

OPEN product_cursor;
FETCH NEXT FROM product_cursor INTO @ProductId;

WHILE @@FETCH_STATUS = 0 AND @ImageIndex <= @MaxImages
BEGIN
    -- Construct the image file path based on sequential index
    SET @ImagePath = N'D:\repros\VS2022-lab300\src\Products\wwwroot\images\product' + CAST(@ImageIndex AS NVARCHAR(10)) + N'.png';
    
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
GO

-- Alternatively, use the PowerShell script or API endpoint for loading images
PRINT '';
PRINT 'To load images, use one of the following methods:';
PRINT '1. PowerShell script: Products/SQL/LoadImages.ps1';
PRINT '2. API endpoint: PUT /api/Product/{id}/image';
PRINT '3. OPENROWSET (requires special permissions - see comments in this file)';
GO
