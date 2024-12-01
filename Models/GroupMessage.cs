using System;
using System.ComponentModel.DataAnnotations;

namespace Message.Models
{
    public class GroupMessage
    {
        [Key]
        public int MessageId { get; set; }
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        
        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User Sender { get; set; }
    }
}
