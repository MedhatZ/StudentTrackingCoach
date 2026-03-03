using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models.ViewModels;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _db;

        public TaskService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<TaskViewModel>> GetPendingTasksAsync(string advisorId)
        {
            var query = _db.Interventions
                .AsNoTracking()
                .Where(i => i.Status == "Approved");

            if (!string.IsNullOrWhiteSpace(advisorId))
            {
                query = query.Where(i => i.AdvisorId == advisorId);
            }

            var interventions = await query
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            return interventions
                .Select(MapToTaskViewModel)
                .ToList();
        }

        public async Task<List<TaskViewModel>> GetCompletedTasksAsync(string advisorId)
        {
            var query = _db.Interventions
                .AsNoTracking()
                .Where(i => i.Status == "Completed");

            if (!string.IsNullOrWhiteSpace(advisorId))
            {
                query = query.Where(i => i.AdvisorId == advisorId);
            }

            var interventions = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return interventions
                .Select(MapToTaskViewModel)
                .ToList();
        }

        public async Task<bool> CompleteTaskAsync(int taskId)
        {
            var intervention = await _db.Interventions
                .FirstOrDefaultAsync(i => i.Id == taskId);

            if (intervention == null)
            {
                return false;
            }

            intervention.Status = "Completed";
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateTaskFromInterventionAsync(int interventionId)
        {
            var intervention = await _db.Interventions
                .FirstOrDefaultAsync(i => i.Id == interventionId);

            if (intervention == null)
            {
                return false;
            }

            if (intervention.Status == "Approved")
            {
                return true;
            }

            if (intervention.Status == "Rejected")
            {
                return false;
            }

            intervention.Status = "Approved";
            await _db.SaveChangesAsync();
            return true;
        }

        private static TaskViewModel MapToTaskViewModel(Models.Intervention intervention)
        {
            var studyGuide = DeserializeStudyGuide(intervention.Content);
            var dueDate = studyGuide?.FollowUpDate;
            var focusAreas = studyGuide?.FocusAreas ?? string.Empty;

            return new TaskViewModel
            {
                Id = intervention.Id,
                Title = $"{intervention.Type} Follow-up",
                Description = string.IsNullOrWhiteSpace(focusAreas)
                    ? "Follow up with student on approved intervention."
                    : focusAreas,
                StudentId = intervention.StudentId,
                StudentName = string.IsNullOrWhiteSpace(intervention.StudentName)
                    ? $"Student {intervention.StudentId}"
                    : intervention.StudentName,
                DueDate = dueDate,
                Status = intervention.Status,
                Type = intervention.Type,
                CreatedAt = intervention.CreatedAt
            };
        }

        private static RecommendedStudyGuideViewModel? DeserializeStudyGuide(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<RecommendedStudyGuideViewModel>(content);
            }
            catch
            {
                return null;
            }
        }
    }
}
