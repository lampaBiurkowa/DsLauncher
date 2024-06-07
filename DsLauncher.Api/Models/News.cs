using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Api.Models;

public class News : Entity, ISoftDelete, ITimeStamped
{
    public required string Content {get; set;}
    public required string Image {get; set;}
    public required string Title {get; set;}
    public required string Summary {get; set; }
    public Product? Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid? ProductGuid { get; set; }
    [DsLong]
    public long? ProductId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
