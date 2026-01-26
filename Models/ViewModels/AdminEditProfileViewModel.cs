namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminEditProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // 🔥 FIX: Domain IDs are LONG
        public int? StudentId { get; set; }
        public int? AdvisorId { get; set; }

        public IList<string> Roles { get; set; } = new List<string>();

        // UI control flags
        public bool CanEditStudent { get; set; }
        public bool CanEditAdvisor { get; set; }
    }
}
