using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Review : Entity, ISoftDelete, ITimeStamped
{
    public required string Content { get; set; }
    public DateTime Date {get; set;}
    public required Product Product { get; set; }
    [DsId(nameof(Models.Product))]
    public DsId ProductDsId { get; set; }
    public int Rate { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
