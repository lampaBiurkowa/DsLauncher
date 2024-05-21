using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Package : Entity, ISoftDelete, ITimeStamped
{
    public DateTime Date { get; set; }
    public required Product Product { get; set; }
    public long ProductId { get; set; }
    public required string Description { get; set; }
    public required string ExePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
