using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class Review : Entity, ISoftDelete, ITimeStamped
{
    public required string Content { get; set; }
    public DateTime Date {get; set;}
    public required Product Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid ProductGuid { get; set; }
    [DsLong]
    public long ProductId { get; set; }
    public int Rate { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
