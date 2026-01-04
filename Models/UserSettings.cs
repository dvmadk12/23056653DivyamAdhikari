namespace JUpdate.Models
{
    public class UserSettings
    {
        public int Id { get; set; }
        public string? PinHash { get; set; } // Hashed PIN for authentication
        public string Theme { get; set; } = "light"; // light, dark, or custom
        public string? CustomThemeData { get; set; } // JSON for custom theme colors
    }
}

