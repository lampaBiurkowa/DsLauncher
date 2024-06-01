using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Api.Models;

public class App : Product, IDerivedKey
{
    // public Product? Product { get; set; }
    // [DsGuid(nameof(Models.Product))]
    // public Guid ProductGuid { get; set; }
    // [DsLong]
    // public long ProductId { get; set; }
}