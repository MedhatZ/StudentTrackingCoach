using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("Student", Schema = "dbo")]
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long StudentId { get; set; }

        public int InstitutionId { get; set; }

        public string? EnrollmentStatus { get; set; }

        public bool? IsFirstGen { get; set; }

        public bool? IsWorking { get; set; }

        public string? PreferredModality { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
