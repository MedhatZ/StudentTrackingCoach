using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models.RUM
{
    public class PageViewModel
    {
        [Required]
        [MaxLength(300)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? PageTitle { get; set; }

        public double? TtfbMs { get; set; }
        public double? FirstContentfulPaintMs { get; set; }
        public double? TimeToInteractiveMs { get; set; }
        public double? PageLoadCompleteMs { get; set; }

        [MaxLength(100)]
        public string? Browser { get; set; }

        [MaxLength(100)]
        public string? DeviceType { get; set; }

        [MaxLength(40)]
        public string? ScreenSize { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        [MaxLength(120)]
        public string? SessionId { get; set; }
    }
}
