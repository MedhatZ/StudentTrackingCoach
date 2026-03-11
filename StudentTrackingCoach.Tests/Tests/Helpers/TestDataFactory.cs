using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Tests.Tests.Helpers
{
    public static class TestDataFactory
    {
        public static Student CreateStudent(long id, bool? firstGen = null, bool? isWorking = null, string? status = "Active")
        {
            return new Student
            {
                StudentId = id,
                InstitutionId = 1,
                IsFirstGen = firstGen,
                IsWorking = isWorking,
                EnrollmentStatus = status,
                PreferredModality = "Online",
                CreatedAt = DateTime.UtcNow
            };
        }

        public static AdvisorNote CreateAdvisorNote(long studentId, string notes, DateTime? createdAt = null)
        {
            return new AdvisorNote
            {
                StudentId = studentId,
                AdvisorUserId = "advisor-1",
                ActionTaken = "Note",
                Notes = notes,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }

        public static Intervention CreateIntervention(long studentId, string status, string content = "{}")
        {
            return new Intervention
            {
                StudentId = studentId,
                AdvisorId = "advisor-1",
                Type = "Study Guide",
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Status = status,
                StudentName = $"Student {studentId}"
            };
        }

        public static RecommendedStudyGuideViewModel CreateGuideVm(long studentId)
        {
            return new RecommendedStudyGuideViewModel
            {
                StudentId = studentId,
                StudentName = $"Student {studentId}",
                CourseName = "Course",
                CurrentGrade = "70",
                FocusAreas = "Focus",
                StudySchedule = "Schedule",
                StudyTechniques = "Techniques",
                Resources = "Resources",
                FollowUpDate = DateTime.Today.AddDays(7),
                ExpectedOutcome = "Outcome"
            };
        }
    }
}
