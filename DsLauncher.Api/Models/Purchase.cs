using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class Purchase : Entity
{
    public Product Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid ProductGuid { get; set; }
    [DsLong]
    public long ProductId { get; set; }
    public Guid TransactionGuid { get; set; }
}
