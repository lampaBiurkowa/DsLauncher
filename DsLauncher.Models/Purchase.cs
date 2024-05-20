using DibBase.ModelBase;

namespace DsLauncher.Models;

public class Purchase : Entity
{
    public DateTime Date { get; set; }
    public required Product Product { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public float Value { get; set; }
}
