using System.ComponentModel.DataAnnotations;

namespace Message.Models
{
    public class Messages
    {
        [Key]
        public int MessageId { get; set; } // Primary Key

        public int SenderId { get; set; }  // Foreign Key referencing the User table
        public int ReceiverId { get; set; } // Foreign Key referencing the User table

        public string MessageText { get; set; } // Content of the message

        public DateTime Timestamp { get; set; } // Date and time the message was sent
    }
}
