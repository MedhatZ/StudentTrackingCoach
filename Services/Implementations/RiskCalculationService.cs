using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class RiskCalculationService : IRiskCalculationService
    {
        private readonly ApplicationDbContext _db;

        // Risk thresholds (hardcoded - no DB changes)
        private const int HIGH_RISK_PRIORITY = 1;
        private const int MEDIUM_RISK_PRIORITY = 2;
        private const int LOW_RISK_PRIORITY = 3;

        public RiskCalculationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<int> CalculateStudentRiskPriorityAsync(long studentId)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return LOW_RISK_PRIORITY;

            // Risk logic based on EXISTING fields only
            bool hasRiskFactors = false;
            bool hasMultipleRiskFactors = false;

            // Check existing demographic fields as risk indicators
            if (student.IsFirstGen == true)
                hasRiskFactors = true;

            if (student.IsWorking == false) // Not working might indicate disengagement
                hasRiskFactors = true;

            if (student.EnrollmentStatus == "At Risk" || student.EnrollmentStatus == "Probation")
                hasMultipleRiskFactors = true;

            // Get all recent notes first (client evaluation after ToListAsync)
            var recentNotesList = await _db.AdvisorNotes
                .IgnoreAutoIncludes()
                .Where(n => n.StudentId == studentId &&
                       n.CreatedAt > DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            // Count notes
            var recentNotesCount = recentNotesList.Count;

            // Check for negative keywords in memory (after data is loaded)
            var negativeKeywordsCount = recentNotesList
                .Count(n => n.Notes != null &&
                    (n.Notes.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                     n.Notes.Contains("struggl", StringComparison.OrdinalIgnoreCase) ||
                     n.Notes.Contains("miss", StringComparison.OrdinalIgnoreCase) ||
                     n.Notes.Contains("attend", StringComparison.OrdinalIgnoreCase)));

            // Use these for risk calculation
            if (recentNotesCount > 3 || negativeKeywordsCount > 2)
                hasMultipleRiskFactors = true;
            else if (recentNotesCount > 1 || negativeKeywordsCount > 0)
                hasRiskFactors = true;

            // Determine risk priority
            if (hasMultipleRiskFactors)
                return HIGH_RISK_PRIORITY;

            if (hasRiskFactors)
                return MEDIUM_RISK_PRIORITY;

            return LOW_RISK_PRIORITY;
        }

        public async Task<string> CalculateStudentRiskLevelAsync(long studentId)
        {
            var priority = await CalculateStudentRiskPriorityAsync(studentId);

            return priority switch
            {
                HIGH_RISK_PRIORITY => "High",
                MEDIUM_RISK_PRIORITY => "Medium",
                _ => "Low"
            };
        }

        public async Task<List<string>> GetRiskDriversAsync(long studentId)
        {
            var drivers = new List<string>();
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return drivers;

            // Build risk drivers from existing data
            if (student.IsFirstGen == true)
                drivers.Add("First generation student");

            if (student.IsWorking == false)
                drivers.Add("Not employed - possible engagement risk");

            if (student.EnrollmentStatus == "At Risk" || student.EnrollmentStatus == "Probation")
                drivers.Add($"Enrollment status: {student.EnrollmentStatus}");

            // Get recent notes for risk drivers
            var recentNotesList = await _db.AdvisorNotes
                .IgnoreAutoIncludes()
                .Where(n => n.StudentId == studentId &&
                       n.CreatedAt > DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            if (recentNotesList.Count > 0)
            {
                drivers.Add($"Recent advisor notes ({recentNotesList.Count} in last 30 days)");

                // Check for keywords in memory
                if (recentNotesList.Any(n => n.Notes != null &&
                    (n.Notes.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                     n.Notes.Contains("struggl", StringComparison.OrdinalIgnoreCase))))
                {
                    drivers.Add("Academic concerns mentioned in advisor notes");
                }
            }

            return drivers;
        }

        // For demo/presentation purposes only - simulates grades without DB changes
        public async Task<decimal?> GetSimulatedAverageGradeAsync(long studentId)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return null;

            // Generate demo grade based on risk factors (for UI demonstration only)
            // This is NOT stored in database
            if (student.IsFirstGen == true && student.IsWorking == false)
                return 62; // High risk - low grade

            if (student.IsFirstGen == true || student.IsWorking == false)
                return 73; // Medium risk - borderline grade

            return 85; // Low risk - good grade
        }
    }
}
