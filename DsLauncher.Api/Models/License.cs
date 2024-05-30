using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class License : Entity
{
    public required string Salt { get; set; }
    public required string Key { get; set; }
    [DsGuid(nameof(Models.Developer))]
    public Guid DeveloperGuid { get; set; }
    [DsLong]
    public long DeveloperId { get; set; }
    public Developer? Developer { get; set; }
}
