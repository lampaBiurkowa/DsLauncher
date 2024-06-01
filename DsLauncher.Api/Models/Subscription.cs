using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Api.Models;

public class Subscription : Entity
{
    public Developer Developer { get; set; }
    [DsGuid(nameof(Models.Developer))]
    public Guid DeveloperGuid { get; set; }
    [DsLong]
    public long DeveloperId { get; set; }
    public Guid CyclicFeeGuid { get; set; }
    public Guid UserGuid { get; set; }
}
