namespace Void.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }

        public string? ProfilePicture { get; set; }

        public DateTime? LastActive { get; set; }


        public virtual ICollection<Chat> SentChats { get; set; } = new List<Chat>();
        public virtual ICollection<Chat> ReceivedChats { get; set; } = new List<Chat>();

    }
}