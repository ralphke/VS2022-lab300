using BenchmarkDotNet.Attributes;
using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ProductBenchmark
{
    private ProductDataContext _context;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ProductDataContext>()
            .UseInMemoryDatabase("BenchmarkDb")
            .Options;
        _context = new ProductDataContext(options);

        // Seed some test data
        var products = new List<Product>();
        for (int i = 0; i < 1000; i++)
        {
            products.Add(new Product { Name = $"Test Product {i}", Description = $"Description {i}", Price = 10.99m + i, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
        }
        _context.Product.AddRange(products);
        _context.SaveChanges();
    }

    [Benchmark]
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Product.AsNoTracking().ToListAsync();
    }

    [Benchmark]
    public List<Product> GetAllProductsSync()
    {
        return _context.Product.AsNoTracking().ToList();
    }

    [Benchmark]
    public List<Product> GetProductsPaginatedSync()
    {
        return _context.Product.AsNoTracking().OrderBy(p => p.Id).Skip(0).Take(10).ToList();
    }
}