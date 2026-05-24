namespace HomeGroundCoffeeBar.Models;

public class RiderLocation
{
    public int    Id        { get; set; }
    public string OrderId   { get; set; } = string.Empty;
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}