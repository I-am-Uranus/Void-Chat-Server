namespace VoidPart2.DTOs
{
    public class CreateGroupDTO
    {
        public string Name { get; set; } = string.Empty;
        public List<int> MemberIds { get; set; } = new();
    }
}
