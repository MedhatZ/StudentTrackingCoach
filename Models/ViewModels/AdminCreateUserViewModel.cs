using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminCreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string UserName { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string TemporaryPassword { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";

        // Optional domain links
        public long? StudentId { get; set; }
        public int? AdvisorId { get; set; }

        // UI helper
        public List<string> AvailableRoles { get; set; } = new();
    }
}
