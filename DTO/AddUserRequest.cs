namespace HomeGroundCoffeeBar.DTO;
public class AddUserRequest
{
    public string Name     { get; set; } = string.Empty;
    public string Phone    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role     { get; set; } = "User";
}
