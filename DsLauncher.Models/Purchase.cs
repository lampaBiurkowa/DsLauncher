using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class Purchase : Entity
{
    public DateTime Date { get; set; }
    public required Product Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid ProductGuid { get; set; }
    [DsLong]
    public long ProductId { get; set; }
    public Guid UserId { get; set; }
    public float Value { get; set; }
}
