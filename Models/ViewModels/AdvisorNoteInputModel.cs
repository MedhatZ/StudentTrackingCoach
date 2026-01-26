using System.ComponentModel.DataAnnotations;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdvisorNoteInputModel
    {
        [Required]
        public long StudentId { get; set; }

        [Required]
        [StringLength(200)]
        public string ActionTaken { get; set; } = string.Empty;

        [Required]
        public string Notes { get; set; } = string.Empty;
    }
}
