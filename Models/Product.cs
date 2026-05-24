using System.ComponentModel.DataAnnotations.Schema;
namespace HomeGroundCoffeeBar.Models; 

public class Product
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]  // ← add this
    public decimal Price    { get; set; }

    public string  Image    { get; set; } = string.Empty;
    public string  Category { get; set; } = string.Empty;
    public int     Stock    { get; set; } = 0;
    public bool    IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}