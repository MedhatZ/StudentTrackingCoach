using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models.Ai;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class MockAiRecommendationService : IAiRecommendationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IRiskCalculationService _riskService;

        public MockAiRecommendationService(ApplicationDbContext db, IRiskCalculationService riskService)
        {
            _db = db;
            _riskService = riskService;
        }

        public async Task<AiRecommendationResult> GenerateRecommendationsAsync(long studentId)
        {
            var riskLevel = await _riskService.CalculateStudentRiskLevelAsync(studentId);
            var riskDrivers = await _riskService.GetRiskDriversAsync(studentId);
            var currentGrade = await _riskService.GetSimulatedAverageGradeAsync(studentId);

            var recentNotes = await _db.AdvisorNotes
                .AsNoTracking()
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => n.Notes ?? string.Empty)
                .ToListAsync();

            var signals = new StudentRiskSignals
            {
                StudentId = studentId,
                RiskLevel = riskLevel,
                RiskDrivers = riskDrivers,
                CurrentGrade = currentGrade,
                CourseGrades = BuildMockCourseGrades(currentGrade),
                RecentNotes = recentNotes,
                Attendance = BuildAttendanceEstimate(riskLevel),
                TermAverage = currentGrade
            };

            return await GenerateRecommendationsFromSignalsAsync(signals);
        }

        public Task<AiRecommendationResult> GenerateRecommendationsFromSignalsAsync(StudentRiskSignals signals)
        {
            var riskDriverSummary = signals.RiskDrivers.Any()
                ? string.Join("; ", signals.RiskDrivers.Take(3))
                : "No specific risk drivers identified";

            var result = signals.RiskLevel switch
            {
                "High" => new AiRecommendationResult
                {
                    FocusAreas = $"Risk-driven focus: {riskDriverSummary}\nFirst-gen student needs math support\nAttendance recovery and assignment completion",
                    StudySchedule = "Mon-Fri: 2 hours/day + daily check-ins\nSat: 3-hour tutoring and practice\nSun: Weekly reflection and planner setup",
                    StudyTechniques = "Pomodoro 25/5 with progress log\nActive recall and practice testing\nError analysis after each assignment",
                    Resources = "Tutoring center (3x weekly)\nAdvisor office check-ins\nPeer accountability group",
                    FollowUpDate = DateTime.Today.AddDays(7),
                    ExpectedOutcome = "Stabilize performance and move toward 70%+ within 4 weeks",
                    ConfidenceScore = 0.74
                },
                "Medium" => new AiRecommendationResult
                {
                    FocusAreas = $"Risk-driven focus: {riskDriverSummary}\nWeekly skill-building and consistency\nTargeted support in weakest subjects",
                    StudySchedule = "Mon-Thu: 90 minutes/day\nFri: Weekly check-in and recap\nWeekend: 2-hour concept practice block",
                    StudyTechniques = "Spaced repetition\nWorked examples\nWeekly mistake log and correction loop",
                    Resources = "Tutoring center (weekly)\nWriting/math support lab\nStructured course discussion group",
                    FollowUpDate = DateTime.Today.AddDays(14),
                    ExpectedOutcome = "Increase consistency and raise grade to 75%+",
                    ConfidenceScore = 0.81
                },
                _ => new AiRecommendationResult
                {
                    FocusAreas = $"Risk-driven focus: {riskDriverSummary}\nMaintain progress and deepen mastery\nPrevent performance drift",
                    StudySchedule = "Weekdays: 60 minutes/day\nWeekly enrichment session\nMonthly advisor check-ins",
                    StudyTechniques = "Self-testing\nInterleaved practice\nConcept teaching to peers",
                    Resources = "Course supplemental materials\nOffice hours as needed\nEnrichment project prompts",
                    FollowUpDate = DateTime.Today.AddDays(30),
                    ExpectedOutcome = "Sustain good standing and aim for 85%+",
                    ConfidenceScore = 0.88
                }
            };

            Console.WriteLine(
                $"[AI-Mock] Generated recommendations for Student {signals.StudentId} | Risk={signals.RiskLevel} | Grade={signals.CurrentGrade}");
            Console.WriteLine($"[AI-Mock] Drivers: {string.Join("; ", signals.RiskDrivers)}");

            return Task.FromResult(result);
        }

        private static Dictionary<string, decimal> BuildMockCourseGrades(decimal? currentGrade)
        {
            var baseGrade = currentGrade ?? 72m;
            return new Dictionary<string, decimal>
            {
                ["MAT101"] = Math.Clamp(baseGrade - 8m, 0m, 100m),
                ["ENG102"] = Math.Clamp(baseGrade + 2m, 0m, 100m),
                ["SCI110"] = Math.Clamp(baseGrade - 3m, 0m, 100m)
            };
        }

        private static decimal BuildAttendanceEstimate(string riskLevel)
        {
            return riskLevel switch
            {
                "High" => 68m,
                "Medium" => 80m,
                _ => 92m
            };
        }
    }
}
