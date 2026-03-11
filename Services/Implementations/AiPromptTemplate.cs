using System.Text;
using StudentTrackingCoach.Models.Ai;

namespace StudentTrackingCoach.Services.Implementations
{
    public static class AiPromptTemplate
    {
        public static string SystemPrompt =>
            "You are an academic success coach. Generate a personalized study plan for a student at risk. " +
            "Return ONLY valid JSON with fields: focusAreas, studySchedule, studyTechniques, resources, followUpDate, expectedOutcome, confidenceScore.";

        public static string BuildUserPrompt(StudentRiskSignals signals)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Generate a personalized study recommendation from the following student signals.");
            sb.AppendLine($"StudentId: {signals.StudentId}");
            sb.AppendLine($"RiskLevel: {signals.RiskLevel}");
            sb.AppendLine($"RiskDrivers: {string.Join("; ", signals.RiskDrivers)}");
            sb.AppendLine($"CurrentGrade: {(signals.CurrentGrade?.ToString("0.0") ?? "N/A")}");
            sb.AppendLine($"TermAverage: {(signals.TermAverage?.ToString("0.0") ?? "N/A")}");
            sb.AppendLine($"Attendance: {(signals.Attendance?.ToString("0.0") ?? "N/A")}");
            sb.AppendLine($"CourseGrades: {string.Join(", ", signals.CourseGrades.Select(kv => $"{kv.Key}:{kv.Value:0.0}"))}");
            sb.AppendLine($"RecentNotes: {string.Join(" | ", signals.RecentNotes.Where(n => !string.IsNullOrWhiteSpace(n)).Take(5))}");
            sb.AppendLine();
            sb.AppendLine("Return JSON only using this shape:");
            sb.AppendLine("{");
            sb.AppendLine("  \"focusAreas\": \"string with newline separators\",");
            sb.AppendLine("  \"studySchedule\": \"string with newline separators\",");
            sb.AppendLine("  \"studyTechniques\": \"string with newline separators\",");
            sb.AppendLine("  \"resources\": \"string with newline separators\",");
            sb.AppendLine("  \"followUpDate\": \"YYYY-MM-DD\",");
            sb.AppendLine("  \"expectedOutcome\": \"string\",");
            sb.AppendLine("  \"confidenceScore\": 0.0");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
