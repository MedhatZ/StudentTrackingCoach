using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface ITaskService
    {
        Task<List<TaskViewModel>> GetPendingTasksAsync(string advisorId);
        Task<List<TaskViewModel>> GetCompletedTasksAsync(string advisorId);
        Task<bool> CompleteTaskAsync(int taskId);
        Task<bool> CreateTaskFromInterventionAsync(int interventionId);
    }
}
