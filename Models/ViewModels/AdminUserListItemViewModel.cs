namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        // 🔥 FIX: must be settable by controller
        public bool IsDisabled { get; set; }

        public int? StudentId { get; set; }
        public int? AdvisorId { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}
