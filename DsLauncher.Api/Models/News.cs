using DibBase.ModelBase;

namespace DsLauncher.Api.Models;

public class News : Entity, ISoftDelete, ITimeStamped
{
    public required string Content {get; set;}
    public required string Image {get; set;}
    public required string Title {get; set;}
    public required string Summary {get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
