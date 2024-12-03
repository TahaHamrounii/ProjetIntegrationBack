using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Message.Models
{
    public class UserSettings
    {
        [Key]
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public string Theme { get; set; } = "light";
        
        public string Language { get; set; } = "en";

        // Navigation property
        [JsonIgnore]
        public virtual User? User { get; set; }
    }
}
