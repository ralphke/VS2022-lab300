-- Created by GitHub Copilot in SSMS - review carefully before executing

-- 1. Enable preview features (required for VECTOR support)
CREATE OR ALTER PROCEDURE dbo.usp_EnableVectorSupport
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Enable preview features for current database
    ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;
    
    -- Verify compatibility level (must be 170+)
    IF (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) < 170
    BEGIN
        RAISERROR('Database compatibility level must be 170 or higher for VECTOR support', 16, 1);
        RETURN;
    END
    
    PRINT 'Vector support enabled successfully';
    -- Enable the external REST endpoint feature for testing embedding service integration
    exec sp_configure 'show advanced options', 1;
    RECONFIGURE;
    exec sp_configure 'external rest endpoint enabled',1;
    RECONFIGURE;
    exec sp_configure 'show advanced options', 0;
    RECONFIGURE;
END;
GO

-- 2. Add vector columns to Products table
CREATE OR ALTER PROCEDURE dbo.usp_AddVectorColumnsToProducts
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Add product description vector (1536 dimensions - typical for OpenAI embeddings)
    IF NOT EXISTS (SELECT 1 FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Products') 
                   AND name = 'DescriptionVector')
    BEGIN
        ALTER TABLE dbo.Products 
        ADD DescriptionVector VECTOR(1536, FLOAT32) NULL;
        PRINT 'Added DescriptionVector column';
    END
    
    -- Add product name vector (smaller dimension for titles)
    IF NOT EXISTS (SELECT 1 FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Products') 
                   AND name = 'NameVector')
    BEGIN
        ALTER TABLE dbo.Products 
        ADD NameVector VECTOR(384, FLOAT32) NULL;
        PRINT 'Added NameVector column';
    END
    
    PRINT 'Vector columns added successfully';
END;
GO