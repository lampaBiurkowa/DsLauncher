using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Api.Models;

public class Product : Entity, IAudited, ITimeStamped, ISoftDelete
{
    [DsGuid(nameof(Models.Developer))]
    public Guid DeveloperGuid { get; set; }
    [DsLong]
    public long DeveloperId { get; set; }
    public Developer? Developer { get; set; }
    public required string Description { get; set; }
    public required string Name { get; set; }
    public float Price { get; set; }
    public required string Tags { get; set; }
    public int ImageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public List<string> GetFieldsToAudit() => [nameof(Price)];
}
