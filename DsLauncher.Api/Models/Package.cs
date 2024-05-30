using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class Package : Entity, ISoftDelete, ITimeStamped
{
    public Product? Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid ProductGuid { get; set; }
    [DsLong]
    public long ProductId { get; set; }
    public string? WindowsExePath { get; set; }
    public string? LinuxExePath { get; set; }
    public string? MacExePath { get; set; }
    public required string Version { get; set; }
    public uint MinRamMib { get; set; }
    public string? MinCpu { get; set; }
    public uint MinDiskMib { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
