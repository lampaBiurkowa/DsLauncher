namespace DsLauncher.Api.Models;

public class Game : Product, IDerivedKey, INdibable
{    
    public ContentClassification ContentClassification { get; set; }
}