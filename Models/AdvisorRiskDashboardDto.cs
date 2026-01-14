using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTrackingCoach.Models
{
    public class AdvisorRiskDashboardDto
    {
        // 🔑 REQUIRED for advisor scoping & LINQ filtering
        public long StudentId { get; set; }

        // 👤 Student identity (UI-only for now)
        [NotMapped]
        public string? FirstName { get; set; }

        [NotMapped]
        public string? LastName { get; set; }

        // 📊 Academic / risk metrics (future use)
        [NotMapped]
        public decimal? AverageScore { get; set; }

        // ✅ THESE MUST EXIST IN THE SQL VIEW
        public int RiskScore { get; set; }
        public string? RiskLevel { get; set; }

        // 🧠 Risk explanations (must exist in SQL)
        public string? PrimaryRiskDriver { get; set; }
        public string? SecondaryRiskDriver { get; set; }

        // ⏱ When the risk was last evaluated (must exist in SQL)
        public DateTime LastEvaluatedAt { get; set; }
    }
}
