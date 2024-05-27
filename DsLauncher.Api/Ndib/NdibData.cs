namespace DsLauncher.Api.Ndib;

public class NdibData
{
    public required string Name { get ; set; }
    public required string Description { get ; set; }
    public required string WindowsExePath { get ; set; }
    public required string LinuxExePath { get ; set; }
    public required string MacExePath { get ; set; }
    public required float Price { get ; set; }
    public List<string> Tags { get ; set; } = [];
    public required string Version { get; set; }
    public required string Icon { get ; set; }
    public required string Background { get ; set; }
    public List<string> Images { get ; set; } = [];
    public required string MinCpu { get ; set; }
    public required uint MinRamMib { get ; set; }
    public required uint MinDiskMib { get ; set; }
}