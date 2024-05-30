namespace DsLauncher.Events;

public class PurchasedEvent
{
    public Guid ProductGuid { get; set; }
    public Guid UserGuid { get; set; }
}