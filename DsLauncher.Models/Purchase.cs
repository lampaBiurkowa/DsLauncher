using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Purchase : Entity
{
    public DateTime Date { get; set; }
    public required Product Product { get; set; }
    [DsId(nameof(Models.Product))]
    public DsId ProductDsId { get; set; }
    public Guid UserId { get; set; }
    public float Value { get; set; }
}
