namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminEditRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public List<RoleCheckboxItem> Roles { get; set; } = new();
    }

    public class RoleCheckboxItem
    {
        public string RoleName { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
    }
}
