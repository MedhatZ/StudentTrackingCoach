using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("Student", Schema = "dbo")]
    public class Student : ITenantScopedEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long StudentId { get; set; }

        public int InstitutionId { get; set; }
        public int TenantId { get; set; } = 1;

        public string? EnrollmentStatus { get; set; }

        public bool? IsFirstGen { get; set; }

        public bool? IsWorking { get; set; }

        public string? PreferredModality { get; set; }

        public DateTime? CreatedAt { get; set; }

        // ============================
        // FUTURE IDENTITY FIELDS
        // (NOT IN DB YET)
        // ============================
        [NotMapped]
        public string? FirstName { get; set; }

        [NotMapped]
        public string? LastName { get; set; }

        [NotMapped]
        public string FullName => $"Student #{StudentId}";

        public ICollection<AdvisorStudent> AdvisorStudents { get; set; } = new List<AdvisorStudent>();
    }
}
