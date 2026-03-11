namespace StudentTrackingCoach.Models
{
    public interface ITenantScopedEntity
    {
        int TenantId { get; set; }
    }
}
