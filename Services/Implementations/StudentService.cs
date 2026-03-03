using Microsoft.EntityFrameworkCore;
using StudentTrackingCoach.Data;
using StudentTrackingCoach.Models;
using StudentTrackingCoach.Services.Interfaces;

namespace StudentTrackingCoach.Services.Implementations
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _db;
        
        public StudentService(ApplicationDbContext db)
        {
            _db = db;
        }
        
        public async Task<Student?> GetStudentByIdAsync(long studentId)
        {
            return await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }
        
        public async Task<List<Student>> GetAllStudentsAsync(bool highRiskOnly = false)
        {
            var query = _db.Students.AsNoTracking();
            
            if (highRiskOnly)
            {
                // Temporary filter - will be replaced with real risk logic later
                query = query.Where(s => s.IsFirstGen == true || s.IsWorking == false);
            }
            
            return await query.ToListAsync();
        }
        
        public async Task<Student?> GetStudentWithDetailsAsync(long studentId)
        {
            return await _db.Students
                .Include(s => s.AdvisorStudents)
                    .ThenInclude(a => a.Advisor)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }
    }
}
