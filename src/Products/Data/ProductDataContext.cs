using DataEntities;
using Microsoft.EntityFrameworkCore;

namespace Products.Data;

public class ProductDataContext : DbContext
{
    public ProductDataContext(DbContextOptions<ProductDataContext> options)
      : base(options)
    {
    }

    public DbSet<Product> Product { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    base.OnModelCreating(modelBuilder);

        // Configure Product entity for SQL Server
 modelBuilder.Entity<Product>(entity =>
  {
            entity.ToTable("Products", "dbo");
            entity.HasKey(e => e.Id);
   entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
    entity.Property(e => e.Description).HasMaxLength(1000);
       entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
      entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImageData).HasColumnType("varbinary(max)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
          entity.Property(e => e.ModifiedDate).HasDefaultValueSql("GETUTCDATE()");

    entity.HasIndex(e => e.Name).HasDatabaseName("IX_Products_Name");
            entity.HasIndex(e => e.Price).HasDatabaseName("IX_Products_Price");
});
    }
}

public static class Extensions
{
    public static async Task InitializeDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProductDataContext>();
        var logger = services.GetRequiredService<ILogger<ProductDataContext>>();

        try
        {
   // Ensure database exists (for LocalDB)
     await context.Database.EnsureCreatedAsync();
 logger.LogInformation("Database connection successful.");

     // Seed data if empty
    if (!await context.Product.AnyAsync())
    {
     await DbInitializer.InitializeAsync(context, logger);
        }
     }
        catch (Exception ex)
        {
     logger.LogError(ex, "An error occurred while initializing the database.");
          throw;
        }
    }
}

public static class DbInitializer
{
    public static async Task InitializeAsync(ProductDataContext context, ILogger logger)
    {
        logger.LogInformation("Seeding initial product data...");

        var products = new List<Product>
{
            // Note: images will be loaded into ImageData via LoadImages.ps1 script
       // This enables Scenario 2: Database image serving via /api/Product/{id}/image
  new Product { Name = "Solar Powered Flashlight", Description = "A fantastic product for outdoor enthusiasts", Price = 19.99m, ImageUrl = "images/product1.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Product { Name = "Hiking Poles", Description = "Ideal for camping and hiking trips", Price = 24.99m, ImageUrl = "images/product2.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
     new Product { Name = "Outdoor Rain Jacket", Description = "This product will keep you warm and dry in all weathers", Price = 49.99m, ImageUrl = "images/product3.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
    new Product { Name = "Survival Kit", Description = "A must-have for any outdoor adventurer", Price = 99.99m, ImageUrl = "images/product4.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
new Product { Name = "Outdoor Backpack", Description = "This backpack is perfect for carrying all your outdoor essentials", Price = 39.99m, ImageUrl = "images/product5.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
      new Product { Name = "Camping Cookware", Description = "This cookware set is ideal for cooking outdoors", Price = 29.99m, ImageUrl = "images/product6.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
     new Product { Name = "Camping Stove", Description = "This stove is perfect for cooking outdoors", Price = 49.99m, ImageUrl = "images/product7.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
     new Product { Name = "Camping Lantern", Description = "This lantern is perfect for lighting up your campsite", Price = 19.99m, ImageUrl = "images/product8.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
  new Product { Name = "Camping Tent", Description = "This tent is perfect for camping trips", Price = 99.99m, ImageUrl = "images/product9.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        context.Product.AddRange(products);
   await context.SaveChangesAsync();

        // Load images into ImageData
        await LoadImagesAsync(context, logger);

        logger.LogInformation("Seeded {Count} products successfully.", products.Count);
        logger.LogInformation("Images loaded into ImageData column.");
    }

    private static async Task LoadImagesAsync(ProductDataContext context, ILogger logger)
    {
        try
        {
            string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            
            if (!Directory.Exists(imagesPath))
            {
                logger.LogWarning("Images directory not found: {Path}", imagesPath);
                return;
            }
            
            var products = await context.Product.OrderBy(p => p.Id).ToListAsync();
            var imageLoadTasks = new List<Task>();
            
            for (int i = 0; i < products.Count && i < 9; i++)
            {
                string imageFile = Path.Combine(imagesPath, $"product{i+1}.png");
                if (File.Exists(imageFile))
                {
                    try
                    {
                        byte[] imageData = await File.ReadAllBytesAsync(imageFile);
                        products[i].ImageData = imageData;
                        logger.LogInformation("Loaded image for product {Id}: {File}", products[i].Id, imageFile);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to load image: {File}", imageFile);
                    }
                }
                else
                {
                    logger.LogWarning("Image file not found: {File}", imageFile);
                }
            }
            
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during image loading - continuing without images");
        }
    }
}