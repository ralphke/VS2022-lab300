using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Services;

namespace Products.Endpoints;
/// <summary>   
/// Product API endpoints
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");
      
        // Count all products currently in store
        group.MapGet("/count", async (ProductDataContext db) =>
        {
            var count = await db.Product.CountAsync();
      return Results.Ok(count);
        });
   
        // GET all products
        group.MapGet("/", async (ProductDataContext db, int? page = null, int? size = null) =>
        {
            var query = db.Product.OrderBy(p => p.Id);
            
            if (page.HasValue && size.HasValue && page.Value > 0 && size.Value > 0)
            {
                query = (IOrderedQueryable<Product>)query.Skip((page.Value - 1) * size.Value).Take(size.Value);
            }
            
            return await query.ToListAsync();
        })
        .WithName("GetAllProducts")
    .Produces<List<Product>>(StatusCodes.Status200OK);

        // GET product by ID
        group.MapGet("/{productId:int}", async (int productId, ProductDataContext db) =>
        {
  var product = await db.Product.FindAsync(productId);
         return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithName("GetProductById")
     .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

  // GET product image as PNG
        group.MapGet("/{productId:int}/image", async (int productId, ProductDataContext db) =>
     {
            var product = await db.Product.FindAsync(productId);
   
            if (product is null)
         return Results.NotFound();

    if (product.ImageData is null || product.ImageData.Length == 0)
        return Results.NotFound(new { message = "No image data available" });

    return Results.File(product.ImageData, "image/png");
        })
.WithName("GetProductImage")
        .Produces(StatusCodes.Status200OK, contentType: "image/png")
        .Produces(StatusCodes.Status404NotFound);

        // Debug endpoint: Check image configuration
     group.MapGet("/debug/images", async (ProductDataContext db) =>
        {
            var products = await db.Product
  .Select(p => new
             {
        p.Id,
        p.Name,
p.ImageUrl,
             HasImageData = p.ImageData != null && p.ImageData.Length > 0,
        ImageDataSize = p.ImageData != null ? p.ImageData.Length : 0
      })
          .ToListAsync();

            return Results.Ok(new
       {
  TotalProducts = products.Count,
           ProductsWithImageUrl = products.Count(p => !string.IsNullOrEmpty(p.ImageUrl)),
     ProductsWithImageData = products.Count(p => p.HasImageData),
         Products = products
        });
     })
        .WithName("DebugImages")
        .Produces(StatusCodes.Status200OK);

 // POST to create a new product
    group.MapPost("/", async (Product product, ProductDataContext db, IEmbeddingService embeddingService) =>
 {
   product.CreatedDate = DateTime.UtcNow;
    product.ModifiedDate = DateTime.UtcNow;
    product.DescriptionEmbedding = await embeddingService.EmbedTextAsync(BuildEmbeddingText(product));
  
            db.Product.Add(product);
            await db.SaveChangesAsync();
 return Results.Created($"/api/Product/{product.Id}", product);
  })
 .WithName("CreateProduct")
   .Produces<Product>(StatusCodes.Status201Created);

   // PUT to update a product
      group.MapPut("/{productId:int}", async (int productId, Product updatedProduct, ProductDataContext db, IEmbeddingService embeddingService) =>
        {
     var product = await db.Product.FindAsync(productId);
       if (product is null) return Results.NotFound();

     product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
           product.Details = updatedProduct.Details;
  product.Price = updatedProduct.Price;
        product.ImageUrl = updatedProduct.ImageUrl;
        product.DescriptionEmbedding = await embeddingService.EmbedTextAsync(BuildEmbeddingText(updatedProduct));
 
 // Update image data if provided
   if (updatedProduct.ImageData != null)
    {
       product.ImageData = updatedProduct.ImageData;
            }
      
     product.ModifiedDate = DateTime.UtcNow;

          await db.SaveChangesAsync();
     return Results.NoContent();
        })
.WithName("UpdateProduct")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // PUT to upload product image
  group.MapPut("/{productId:int}/image", async (int productId, IFormFile file, ProductDataContext db) =>
        {
     var product = await db.Product.FindAsync(productId);
  if (product is null) return Results.NotFound();

    if (file.Length == 0)
              return Results.BadRequest(new { message = "Empty file" });

   // Validate file type
            if (!file.ContentType.StartsWith("image/"))
       return Results.BadRequest(new { message = "File must be an image" });

   using var memoryStream = new MemoryStream();
await file.CopyToAsync(memoryStream);
      product.ImageData = memoryStream.ToArray();
    product.ModifiedDate = DateTime.UtcNow;

    await db.SaveChangesAsync();
   return Results.NoContent();
     })
    .WithName("UploadProductImage")
      .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .DisableAntiforgery();

        // DELETE to remove a product
   group.MapDelete("/{productId:int}", async (int productId, ProductDataContext db) =>
     {
   var product = await db.Product.FindAsync(productId);
            if (product is null) return Results.NotFound();

         db.Product.Remove(product);
      await db.SaveChangesAsync();
   return Results.NoContent();
        })
        .WithName("DeleteProduct")
     .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

   private static string BuildEmbeddingText(Product product)
   {
      return string.Join(' ', new[] { product.Name, product.Description, product.Details }.Where(v => !string.IsNullOrWhiteSpace(v)));
   }
}
