namespace DsLauncher.Api.Options;

public class LauncherOptions
{
    public const string SECTION = "Launcher";

    public float DeveloperAccessPrice { get; set; }
    public TimeSpan CyclicPaymentInterval { get; set; }
}