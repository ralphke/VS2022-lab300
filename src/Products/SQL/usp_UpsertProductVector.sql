-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE OR ALTER PROCEDURE dbo.usp_UpsertProductVector
    @ProductId INT,
    @DescriptionVector NVARCHAR(MAX) = NULL,  -- JSON array string
    @NameVector NVARCHAR(MAX) = NULL          -- JSON array string
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        UPDATE dbo.Products
        SET 
            DescriptionVector = CASE WHEN @DescriptionVector IS NOT NULL 
                                     THEN CAST(@DescriptionVector AS VECTOR(1536, FLOAT32)) 
                                     ELSE DescriptionVector END,
            NameVector = CASE WHEN @NameVector IS NOT NULL 
                              THEN CAST(@NameVector AS VECTOR(384, FLOAT32)) 
                              ELSE NameVector END
        WHERE Products.Id = @ProductId;
        
        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Product with ID %d not found', 16, 1, @ProductId);
            RETURN;
        END
        
        PRINT 'Vector updated successfully for ProductId: ' + CAST(@ProductId AS VARCHAR(10));
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO