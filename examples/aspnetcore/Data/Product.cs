// Models/Product.cs
namespace SimpleOpenTelemetry.Examples.AspNetCore.Data;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}