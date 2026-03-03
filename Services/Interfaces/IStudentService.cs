using StudentTrackingCoach.Models;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IStudentService
    {
        Task<Student?> GetStudentByIdAsync(long studentId);
        Task<List<Student>> GetAllStudentsAsync(bool highRiskOnly = false);
        Task<Student?> GetStudentWithDetailsAsync(long studentId);
    }
}
