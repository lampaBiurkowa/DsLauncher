using System.Text;
using DsLauncher.Infrastructure;
using DsLauncher.Models;
public class Generator
{
    private static readonly Random random = new Random();
    private const string DefaultCharacterSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    static string LoremIpsum(int minWords = 10, int maxWords= 20,
    int minSentences=1, int maxSentences=3,
    int numParagraphs=1)
    {

        var words = new[]{"lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
        "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
        "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"};

        var rand = new Random();
        int numSentences = rand.Next(maxSentences - minSentences)
            + minSentences + 1;
        int numWords = rand.Next(maxWords - minWords) + minWords + 1;

        StringBuilder result = new StringBuilder();

        for (int p = 0; p < numParagraphs; p++)
        {
            for (int s = 0; s < numSentences; s++)
            {
                for (int w = 0; w < numWords; w++)
                {
                    if (w > 0) { result.Append(" "); }
                    result.Append(words[rand.Next(words.Length)]);
                }
                result.Append(". ");
            }
        }

        return result.ToString();
    }

    public static bool GenerateBool() => random.Next(2) == 1;

    public static string GenerateString(int length = 20, string characterSet = DefaultCharacterSet)
    {
        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.");
        }

        StringBuilder stringBuilder = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int randomIndex = random.Next(0, characterSet.Length);
            char randomChar = characterSet[randomIndex];
            stringBuilder.Append(randomChar);
        }

        return stringBuilder.ToString();
    }

    public static int GenerateInt(int maxValue = 100) => random.Next(maxValue);

    public static Guid GenerateGuid() => Guid.NewGuid();

    static int developerUserId = 1;

    public static Developer GenerateDeveloper() => new() 
    {
        Name = GenerateString(),
        Description = LoremIpsum(),
        Website = GenerateString(),
        UserGuids = [GenerateGuid(), GenerateGuid()]
    };

    public static News GenerateNews() => new() 
    {
        Content = LoremIpsum(),
        Title = GenerateString(),
        Summary = LoremIpsum(),
        Image = GenerateString()
    };

    public static Product GenerateProduct(Developer developer, string? name = null) => new() 
    {
        Name = name ?? GenerateString(),
        Developer = developer,
        Description = LoremIpsum(),
        Tags = GenerateString(),
        Price = GenerateInt(),
    };

    public static Review GenerateReview(Product product) => new() 
    {
        Product = product,
        Content = LoremIpsum(),
        Rate = GenerateInt(5) + 1,
        UserGuid = GenerateGuid()
    };

    public static Purchase GeneratePurchase(Product product) => new() 
    {
        Date = DateTime.Now,
        Product = product,
        Value = GenerateInt(),
        UserGuid = GenerateGuid()
    };

    public static GameActivity GenerateGameActivity(Product product) => new() 
    {
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddMinutes(5),
        Product = product,
        UserGuid = GenerateGuid()
    };

    public static Package GeneratePackage(Product product, string? exeName = null) => new() 
    {
        ExePath = exeName ?? GenerateString(),
        Product = product,
        Version = GenerateString(),
        IsMac = GenerateBool(),
        IsWin = GenerateBool(),
        IsLinux = GenerateBool(),
        RamMib = (uint)GenerateInt(),
        DiskMib = (uint)GenerateInt(),
        CpuMhz = (uint)GenerateInt()
    };

    public static void Main()
    {
        var db = new DsLauncherContext();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        for (int i = 0; i < 10; i++)
            db.Developer.Add(GenerateDeveloper());
        db.Developer.Add(new()
        {
            Name = "Cardboard Inc",
            Description = "cardboard",
            Website = "www.example.com",
            UserGuids = [GenerateGuid(), GenerateGuid()]
        });
        db.SaveChanges();

        for (int i = 0; i < 10; i++)
            db.News.Add(GenerateNews());
        db.SaveChanges();

        for (int i = 0; i < 10; i++)
            db.Product.Add(GenerateProduct(db.Developer.ToList().ElementAt(random.Next(10))));
        db.Product.Add(GenerateProduct(db.Developer.ToList().ElementAt(random.Next(10)), "asda"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Palmtop Picker"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Frohher"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Space Jumper"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "RAM Engineer"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Arctic Flyer"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Air Drop"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Fire Rescue"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Sea Rescue"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Brainvita"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Moorhuhn Soccer"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "PZPN 18"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "GP vs Superbike"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Team HotWheels DRIFT"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Team HotWheels MOTO X"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Team HotWheels BAJA"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Zalball"));
        db.Product.Add(GenerateProduct(db.Developer.ToList().Last(), "Arkanoid Trzyde"));
        db.SaveChanges();

        for (int i = 0; i < db.Product.Count() * 10; i++)
            db.Review.Add(GenerateReview(db.Product.ToList().ElementAt(random.Next(db.Product.Count()))));
        for (int i = 0; i < 20; i++)
            db.Purchase.Add(GeneratePurchase(db.Product.ToList().ElementAt(random.Next(db.Product.Count()))));
        for (int i = 0; i < 20; i++)
            db.GameActivity.Add(GenerateGameActivity(db.Product.ToList().ElementAt(random.Next(db.Product.Count()))));
        for (int i = 0; i < 20; i++)
            db.Package.Add(GeneratePackage(db.Product.ToList().ElementAt(random.Next(10)))); //nowym nieh ni miesza

        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Palmtop Picker"), "Palmtop Picker.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Frohher"), "frohher.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "RAM Engineer"), "RAMEngineer.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Arctic Flyer"), "START.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Air Drop"), "START.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Fire Rescue"), "START.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Sea Rescue"), "START.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Brainvita"), "WpfApp2.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Space Jumper"), "SpaceJumper.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Moorhuhn Soccer"), "Moorhuhn_Soccer.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "PZPN 18"), "PZPN 18.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "GP vs Superbike"), "bike_nocd_english.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Team HotWheels DRIFT"), "DRIFT.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Team HotWheels MOTO X"), "MOTO.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Team HotWheels BAJA"), "BAJA.EXE"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Zalball"), "rel10.exe"));
        for (int i = 0; i < 10; i++)
            db.Package.Add(GeneratePackage(db.Product.First(p => p.Name == "Arkanoid Trzyde"), "ArkanoidTrzyde.exe"));
        db.SaveChanges();
    }
}