-- Adding the vector support to the TineShopDB database involves several steps. 
-- Below is a sample SQL script that demonstrates how to enable vector support, add necessary columns, create indexes, and test the integration with an embedding service.

-- 1. Enable support
EXEC dbo.usp_EnableVectorSupport;

-- 2. Add columns
EXEC dbo.usp_AddVectorColumnsToProducts;

-- 3. Create indexes
EXEC dbo.usp_CreateProductVectorIndexes;

-- 3a. use this to test your embedding service integration
-- 3a. Test your embedding service integration
/* Corresponding curl request:
curl -X POST http://localhost:8001/embed \
  -H "Content-Type: application/json" \
  -d '{"inputs":"Hello world"}'
*/
-- will not work, as sp_invoke_external_rest_endpoint does only support https requests.
DECLARE @url NVARCHAR(4000) = N'http://localhost:8001/embed';
DECLARE @headers NVARCHAR(4000) = N'{"Content-Type":"application/json"}';
DECLARE @payload NVARCHAR(MAX) = N'{"inputs":"Hello world"}';
DECLARE @ret INT;
DECLARE @response NVARCHAR(MAX);

EXEC @ret = sys.sp_invoke_external_rest_endpoint
    @url = @url,
    @method = 'POST',
    @headers = @headers,
    @payload = @payload,
    @timeout = 30,
    @response = @response OUTPUT;

-- Display results
SELECT 
    @ret AS ReturnCode,
    @response AS Response;

-- Parse response if successful (returns JSON with embedding vector)
IF @ret = 0
BEGIN
    SELECT 
        JSON_VALUE(@response, '$.result.response.status.http.code') AS HttpStatusCode,
        JSON_QUERY(@response, '$.result.response.body') AS ResponseBody;
END
ELSE
BEGIN
    PRINT 'Error occurred during REST endpoint invocation';
END

-- 4. Update a product with vectors (from your embedding service)
EXEC dbo.usp_UpsertProductVector 
    @ProductId = 1,
    @DescriptionVector = '[0.1, 0.2, ..., 0.9]';  -- 1536 dimensions

-- 5. Search for similar products
EXEC dbo.usp_SearchProductsBySimilarity 
    @QueryVector = '[0.15, 0.25, ..., 0.85]',
    @TopN = 5;