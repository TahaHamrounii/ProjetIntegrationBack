using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Message.Models
{
    public class UserSettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        [Required]
        [StringLength(20)]
        public string Theme { get; set; } = "light";
        
        [Required]
        [StringLength(10)]
        public string Language { get; set; } = "en";

        public bool NotifyMessages { get; set; } = true;
        
        public bool NotifyGroups { get; set; } = true;
        
        public bool NotifyCalls { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [JsonIgnore]
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
