namespace HomeGroundCoffeeBar.DTO;

public class RiderLocationRequest
{
    public string OrderId   { get; set; } = string.Empty;
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
}