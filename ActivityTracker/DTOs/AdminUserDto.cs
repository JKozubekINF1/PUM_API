namespace ActivityTracker.DTOs
{
    public class AdminUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public int ActivitiesCount { get; set; }
    }
}