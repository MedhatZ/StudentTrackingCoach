using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    [Table("PendingAction", Schema = "dbo")]
    public class PendingAction
    {
        [Key]
        public long ActionId { get; set; }

        public long? StudentId { get; set; }

        public int? ActionTypeId { get; set; }

        public int? Priority { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Status { get; set; }
    }
}
