using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Developer : Entity, ITimeStamped
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Website { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
