namespace DsLauncher.Api.Models;

public class Game : Product, IDerivedKey
{
    public ContentClassification ContentClassification { get; set; }
}