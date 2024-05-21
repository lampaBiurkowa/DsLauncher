using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Purchase : Entity
{
    public DateTime Date { get; set; }
    public required Product Product { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public float Value { get; set; }
}
