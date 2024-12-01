namespace Message.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime ApplicationDate { get; set; }
        public  string University { get; set; }
        public string Status { get; set; } = "Suspendu";
    }

}
