using DibBase.ModelBase;

namespace DsLauncher.Api.Models;

public class Developer : Entity, ITimeStamped, ISoftDelete
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? ProfileImage { get; set; }
    public List<Guid> UserGuids { get; set; } = [];
    public float SubscriptionPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
