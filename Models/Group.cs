using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Message.Models
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string GroupName { get; set; }
        
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation property for group members
        public ICollection<UserGroup> UserGroups { get; set; }
    }

    public class UserGroup
    {
        public int UserId { get; set; }
        public User User { get; set; }
        
        public int GroupId { get; set; }
        public Group Group { get; set; }
        
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}
