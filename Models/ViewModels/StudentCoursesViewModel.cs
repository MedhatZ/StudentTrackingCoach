using System.Collections.Generic;
using System.Linq;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudentCoursesViewModel
    {
        public long StudentId { get; set; }
        public List<CourseGradeViewModel> Courses { get; set; } = new();
        public decimal CurrentTermAverage => Courses.Any() ? Courses.Average(c => c.CurrentGrade) : 0;
    }

    public class CourseGradeViewModel
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal CurrentGrade { get; set; }
    }
}
