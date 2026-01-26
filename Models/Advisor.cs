using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("Advisor")] // ✅ CRITICAL FIX
    public class Advisor
    {
        [Key]
        public int AdvisorId { get; set; }

        public int InstitutionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }
    }
}
