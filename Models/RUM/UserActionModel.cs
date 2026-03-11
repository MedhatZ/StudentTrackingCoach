using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models.RUM
{
    public class UserActionModel
    {
        [Required]
        [MaxLength(100)]
        public string ActionName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Path { get; set; }

        [MaxLength(100)]
        public string? ElementType { get; set; }

        [MaxLength(120)]
        public string? ElementId { get; set; }

        [MaxLength(120)]
        public string? SessionId { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        public bool Success { get; set; } = true;
    }
}
