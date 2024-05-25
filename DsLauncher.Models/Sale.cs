using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class Sale : Entity, ISoftDelete
{
    public byte Discount { get; set; }
    public DateTime StartDate {get; set;}
    public DateTime EndDate {get; set;}
    public required Product Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid ProductGuid { get; set; }
    [DsLong]
    public long ProductId { get; set; }
    public bool IsDeleted { get; set; }
}
