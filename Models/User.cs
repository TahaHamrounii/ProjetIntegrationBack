using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Message.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [JsonIgnore]
        public string Password { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = "default-avatar.jpg";
        public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public virtual UserSettings? Settings { get; set; }
    }
}
