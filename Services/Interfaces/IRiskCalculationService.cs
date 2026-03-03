namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IRiskCalculationService
    {
        Task<int> CalculateStudentRiskPriorityAsync(long studentId);
        Task<string> CalculateStudentRiskLevelAsync(long studentId);
        Task<List<string>> GetRiskDriversAsync(long studentId);
        Task<decimal?> GetSimulatedAverageGradeAsync(long studentId);
    }
}
