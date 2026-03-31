-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE OR ALTER PROCEDURE dbo.usp_CreateProductVectorIndexes
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create index on description vectors
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                   WHERE name = 'IX_Products_DescriptionVector' 
                   AND object_id = OBJECT_ID('dbo.Products'))
    BEGIN
        CREATE VECTOR INDEX IX_Products_DescriptionVector
        ON dbo.Products(DescriptionVector)
        WITH (
            METRIC = 'cosine',
            TYPE = 'DiskANN'
        );
        PRINT 'Created index on DescriptionVector';
    END
    
    -- Create index on name vectors
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                   WHERE name = 'IX_Products_NameVector' 
                   AND object_id = OBJECT_ID('dbo.Products'))
    BEGIN
        CREATE VECTOR INDEX IX_Products_NameVector
        ON dbo.Products(NameVector)
        WITH (
            METRIC = 'cosine',
            TYPE = 'DiskANN'
        );
        PRINT 'Created index on NameVector';
    END
    
    PRINT 'Vector indexes created successfully';
END;
GO