using StudentTrackingCoach.Models.RUM;

namespace StudentTrackingCoach.Services.Interfaces
{
    public interface IRUMService
    {
        Task TrackPageViewAsync(PageViewModel pageView, string? role);
        Task TrackUserActionAsync(UserActionModel actionModel, string? role);
    }
}
