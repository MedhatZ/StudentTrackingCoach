namespace StudentTrackingCoach.Models.ViewModels
{
    public class TasksIndexViewModel
    {
        public List<TaskViewModel> PendingTasks { get; set; } = new();
        public List<TaskViewModel> CompletedTasks { get; set; } = new();
    }
}
