using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DsLauncher.Models;

public class Package : Entity, ISoftDelete, ITimeStamped
{
    public DateTime Date { get; set; }
    public required Product Product { get; set; }
    [DsGuid(nameof(Models.Product))]
    public Guid ProductGuid { get; set; }
    [DsLong]
    public long ProductId { get; set; }
    public string? Description { get; set; }
    public required string ExePath { get; set; }
    public required string Version { get; set; }
    public bool IsWin { get; set; }
    public bool IsMac { get; set; }
    public bool IsLinux { get; set; }
    public uint RamMib { get; set; }
    public uint CpuMhz { get; set; }
    public uint DiskMib { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
