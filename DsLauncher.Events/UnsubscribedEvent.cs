namespace DsLauncher.Events;

public class UnsubscribedEvent
{
    public Guid DeveloperGuid { get; set; }
    public Guid UserGuid { get; set; }
}