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

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    // Binary image data stored in database
    [JsonIgnore]
    public byte[]? ImageData { get; set; }

    // Base64 encoded image for JSON serialization
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
}


[JsonSerializable(typeof(List<Product>))]
public sealed partial class ProductSerializerContext : JsonSerializerContext
{
}