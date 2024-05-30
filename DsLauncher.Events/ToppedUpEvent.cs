namespace DsLauncher.Events;

public class ToppedUpEvent
{
    public float Value { get; set; }
    public Guid UserGuid { get; set; }
    public Guid CurrencyGuid { get; set; }
}