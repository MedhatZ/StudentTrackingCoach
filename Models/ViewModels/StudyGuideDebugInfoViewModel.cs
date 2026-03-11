namespace StudentTrackingCoach.Models.ViewModels
{
    public class StudyGuideDebugInfoViewModel
    {
        public int GuideId { get; set; }
        public long StudentId { get; set; }
        public int ContentLength { get; set; }
        public string FormatUsed { get; set; } = string.Empty;
        public bool DeserializationSucceeded { get; set; }
        public string? ErrorMessage { get; set; }
        public string RawContentPreview { get; set; } = string.Empty;
    }
}
