using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("AdvisorStudent", Schema = "dbo")]
    public class AdvisorStudent
    {
        [Key]
        public int AdvisorStudentId { get; set; }

        public int AdvisorId { get; set; }
        public long StudentId { get; set; }

        public DateTime AssignedAt { get; set; }

        // Navigation
        public Advisor Advisor { get; set; }
        public Student Student { get; set; }
    }
}

