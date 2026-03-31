-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE OR ALTER PROCEDURE dbo.usp_SearchProductsBySimilarity
    @QueryVector NVARCHAR(MAX),           -- JSON array string
    @SearchType VARCHAR(20) = 'description', -- 'description' or 'name'
    @TopN INT = 10,
    @DistanceMetric VARCHAR(20) = 'cosine'  -- 'cosine', 'euclidean', or 'dot'
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @SearchType = 'description'
    BEGIN
        SELECT TOP (@TopN)
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            VECTOR_DISTANCE(@DistanceMetric, CAST(@QueryVector AS VECTOR(1536, FLOAT32)), p.DescriptionVector) AS SimilarityScore
        FROM dbo.Products p
        WHERE p.DescriptionVector IS NOT NULL
        ORDER BY VECTOR_DISTANCE(@DistanceMetric, CAST(@QueryVector AS VECTOR(1536, FLOAT32)), p.DescriptionVector) ASC;
    END
    ELSE IF @SearchType = 'name'
    BEGIN
        SELECT TOP (@TopN)
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            VECTOR_DISTANCE(@DistanceMetric, CAST(@QueryVector AS VECTOR(384, FLOAT32)), p.NameVector) AS SimilarityScore
        FROM dbo.Products p
        WHERE p.NameVector IS NOT NULL
        ORDER BY VECTOR_DISTANCE(@DistanceMetric, CAST(@QueryVector AS VECTOR(384, FLOAT32)), p.NameVector) ASC;
    END
    ELSE
    BEGIN
        RAISERROR('Invalid @SearchType. Use ''description'' or ''name''', 16, 1);
    END
END;
GO