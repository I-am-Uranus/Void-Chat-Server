namespace VoidPart2.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();

    }
}
