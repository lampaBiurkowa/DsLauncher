using DibBase.ModelBase;

namespace DsLauncher.Models;

public class GameActivity : Entity
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required Product Product { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
}
