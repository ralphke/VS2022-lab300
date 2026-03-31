-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE OR ALTER PROCEDURE dbo.usp_HybridProductSearch
    @QueryVector NVARCHAR(MAX),
    @CategoryFilter INT = NULL,
    @MinPrice DECIMAL(10,2) = NULL,
    @MaxPrice DECIMAL(10,2) = NULL,
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@TopN)
        p.Id,
        p.Name,
        p.Description,
        p.Category,
        p.Price,
        vs.distance AS SimilarityScore
    FROM VECTOR_SEARCH(
        TABLE = dbo.Products AS p,
        COLUMN = DescriptionVector,
        SIMILAR_TO = CAST(@QueryVector AS VECTOR(1536)),
        METRIC = 'cosine',
        TOP_N = 100  -- Get more candidates for filtering
    ) AS vs
    WHERE p.DescriptionVector IS NOT NULL
        AND (@CategoryFilter IS NULL OR p.CategoryId = @CategoryFilter)
        AND (@MinPrice IS NULL OR p.Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR p.Price <= @MaxPrice)
    ORDER BY vs.distance ASC;
END
GO