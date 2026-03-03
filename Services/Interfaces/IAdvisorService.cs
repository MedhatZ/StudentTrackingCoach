using StudentTrackingCoach.Models;
using StudentTrackingCoach.Models.ViewModels;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IAdvisorService
    {
        Task<List<AdvisorRiskDashboardDto>> GetStudentsRequiringAttentionAsync(string advisorId);
        Task<AdvisorStudentDetailViewModel> GetStudentDetailForAdvisorAsync(long studentId);
        Task<StudentCoursesViewModel> GetStudentCoursesAsync(long studentId);
        Task<bool> LogNoteAsync(AdvisorNoteInputModel input, string advisorUserId);
    }
}
