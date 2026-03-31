using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataEntities;

public class Product
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonIgnore]
    public byte[]? ImageData { get; set; }

    [NotMapped]
    [JsonPropertyName("imageDataBase64")]
    public string? ImageDataBase64
    {
        get => ImageData != null ? Convert.ToBase64String(ImageData) : null;
        set => ImageData = value != null ? Convert.FromBase64String(value) : null;
    }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("modifiedDate")]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Vector embedding of the product description for semantic search.
    /// SQL Server stores this as a vector(1536) column (Ada-3 dimension).
    /// </summary>
    [JsonIgnore]
    public float[]? DescriptionEmbedding { get; set; }
}

[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(CustomerProfile))]
[JsonSerializable(typeof(List<CustomerProfile>))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(CreateOrderRequest))]
public sealed partial class ProductSerializerContext : JsonSerializerContext
{
}
