namespace DsLauncher.Api.Ndib;

public class NdibData
{
    public required string Name { get ; set; }
    public required string Description { get ; set; }
    public required string ExePath { get ; set; }
    public required float Price { get ; set; }
    public List<string> Tags { get ; set; } = [];
    public required string Version { get; set; }
    public required string Icon { get ; set; }
    public required string Background { get ; set; }
    public List<string> Images { get ; set; } = [];
    public required uint CpuMhz { get ; set; }
    public required uint RamMib { get ; set; }
    public required uint DiskMib { get ; set; }
    public required bool Windows { get ; set; }
    public required bool Linux { get ; set; }
    public required bool Mac { get ; set; }
}