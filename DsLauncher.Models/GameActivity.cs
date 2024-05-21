using DibBase.ModelBase;

namespace DsLauncher.Models;

public class GameActivity : Entity
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required Product Product { get; set; }
    [DsId(nameof(Models.Product))]
    public DsId ProductDsId { get; set; }
    public Guid UserId { get; set; }
}
