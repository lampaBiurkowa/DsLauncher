using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Product : Entity, IAudited, ITimeStamped, ISoftDelete
{
    [DsId(nameof(Models.Developer))]
    public DsId DeveloperDsId { get; set; }
    public required Developer Developer { get; set; }
    public required string Description { get; set; }
    public required string Name { get; set; }
    public float Price { get; set; }
    public required string Tags {get; set;}
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public List<string> GetFieldsToAudit() => [nameof(Price)];
}
