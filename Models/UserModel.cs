namespace Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ProfilePic { get; set; } = string.Empty;
        public string GoogleId { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public string Role { get; set; } = string.Empty;
        public int Points { get; set; } = 0;
    }
}