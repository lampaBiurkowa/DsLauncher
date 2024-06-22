using System.IO.Compression;
using DibBase.Infrastructure;
using DsLauncher.Api.Models;
using DsStorage.ApiClient;
using DsSftpLib;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using DibBase.Extensions;

namespace DsLauncher.Api.Ndib;

public class NdibService(
    Repository<Package> packageRepo,
    Repository<Product> productRepo,
    Repository<App> appRepo,
    Repository<Game> gameRepo,
    Repository<Developer> developerRepo,
    DsStorageClientFactory dsStorage,
    IDsSftpClient sftpClient)
{
    readonly FileExtensionContentTypeProvider contentTypeProvider = new();
    const string PRODUCTS_BUCKET_NAME = "products";//TODO conf
    
    public async Task<NdibData?> ExtractZipToTemp(IFormFile file, string tempPath, CancellationToken ct)
    {
        var metadataPath = ".ndib/metadata.json";
        if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

        NdibData? ndibData = null;
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            string destinationPath = Path.Combine(tempPath, entry.FullName);
            
            if (entry.FullName.Replace('\\', '/').Equals(metadataPath, StringComparison.OrdinalIgnoreCase))
            {
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                var textContent = await reader.ReadToEndAsync(ct);
                ndibData = JsonConvert.DeserializeObject<NdibData>(textContent);
            }
            else
            {
                var parentDir = Path.GetDirectoryName(destinationPath);
                if (parentDir != null && !Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);
                
                if (!entry.FullName.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        return ndibData;
    }

    public async Task UploadImagesToStorage(NdibData ndibData, string tempPath, Guid productGuid, CancellationToken ct)
    {
        for (int i = 1; i <= ndibData.Images.Count; i++)
        {
            var originalPath = Path.Combine(tempPath, ndibData.Images[i - 1]);
            var fileName = $"{i}{Path.GetExtension(ndibData.Images[i - 1])}";
            await UploadFileToStorage(originalPath, productGuid, fileName, ct);
        }

        if (!string.IsNullOrWhiteSpace(ndibData.Background))
            await UploadFileToStorage(Path.Combine(tempPath, ndibData.Background), productGuid, $"bg{Path.GetExtension(ndibData.Background)}", ct);
        
        if (!string.IsNullOrWhiteSpace(ndibData.Icon))
            await UploadFileToStorage(Path.Combine(tempPath, ndibData.Icon), productGuid, $"icon{Path.GetExtension(ndibData.Icon)}", ct);
        Directory.Delete(tempPath, true);
    }

    void SaveVersionFiles(string tempPath, Package newPackage, Platform? platform = null)
    {   
        sftpClient.UploadDirectory(tempPath, PathsResolver.GetVersionPath(newPackage.ProductGuid, newPackage.Guid, platform));
        var hash = JsonConvert.SerializeObject(GetFileHashes(tempPath));
        sftpClient.UploadStream(GenerateStreamFromString(hash), PathsResolver.GetVersionVerificationHash(newPackage, platform));
        Directory.Delete(tempPath, true);
    }

    public async Task UploadVersion(IFormFile file, Package package, Platform? platform = null, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        await ExtractZipToTemp(file, tempPath, ct);
        SaveVersionFiles(tempPath, package, platform);
    }
    
    static MemoryStream GenerateStreamFromString(string value) => new(Encoding.UTF8.GetBytes(value ?? ""));

    public async Task<Package> PersistPackage(NdibData ndib, long developerId, CancellationToken ct)
    {
        var newProduct = GetProductFromNdibData(ndib, developerId);
        var newPackage = GetPackageFromNdibData(ndib, newProduct);
        await packageRepo.InsertAsync(newPackage, ct);
        await packageRepo.CommitAsync(ct);

        return newPackage;
    }

    public async Task<Package> PersistPackage(NdibData ndib, Product product, CancellationToken ct)
    {
        await AdaptProductToTypeAndUpdate(ndib, product, ct);

        var newPackage = GetPackageFromNdibData(ndib, product);
        newPackage.Product = null;
        newPackage.ProductId = product.Id; //hzd ultra
        await packageRepo.InsertAsync(newPackage, ct);

        await packageRepo.CommitAsync(ct);

        return newPackage;
    }

    public async Task AdaptProductToTypeAndUpdate(NdibData ndib, Product product, CancellationToken ct)
    {
        if (product.ProductType == ProductType.Game)
            product = await gameRepo.GetById(product.Id, ct: ct) ?? throw new($"No corresponding game for product {product.Id}");
        else if (product.ProductType == ProductType.App)
            product = await appRepo.GetById(product.Id, ct: ct) ?? throw new($"No corresponding app for product {product.Id}");

        ApplyNdibDataToProduct(product, ndib);
        if (product.ProductType == ProductType.Game)
            ((Game)product).ContentClassification = ndib.GetContentClassification();
        
        await productRepo.UpdateAsync(product, ct);
    }

    public async Task<Developer?> GetDeveloper(Guid? userGuid, Guid developerId, CancellationToken ct)
    {
        if (userGuid == null) return null;
        var developer = await developerRepo.GetById(developerId.Deobfuscate().Id, ct: ct);
        if (developer?.UserGuids.Contains((Guid)userGuid) != true) return null;
        
        return developer;
    }

    public bool UserIsFromDeveloper(Guid? userGuid, Developer developer)
    {
        if (userGuid == null) return false;
        return developer.UserGuids.Contains((Guid)userGuid);
    }

    public async Task<Dictionary<string, string>> GetVersionVerificationHash(Package package, Platform platform, CancellationToken ct)
    {
        var coreHash = await GetVerificationHashPart(PathsResolver.GetVersionVerificationHash(package), ct);
        var platformHash = await GetVerificationHashPart(PathsResolver.GetVersionVerificationHash(package, platform), ct);

        return coreHash.Concat(platformHash).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    async Task<Dictionary<string, string>> GetVerificationHashPart(string path, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        sftpClient.DownloadFile(stream, path);
        stream.Seek(0, SeekOrigin.Begin);
        return await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: ct) ?? throw new();
    }

    async Task UploadFileToStorage(string srcPath, Guid productGuid, string remoteName, CancellationToken ct)
    {
        using var stream = File.OpenRead(srcPath);
        contentTypeProvider.TryGetContentType(srcPath, out var contentType);
        await dsStorage.CreateClient().Storage_UploadFileToBucketAsync(PRODUCTS_BUCKET_NAME, new(stream, remoteName, contentType), productGuid.ToString(), ct);
    }

    static Product GetProductFromNdibData(NdibData ndib, long developerId) =>
        ndib.IsGame ? GetGameFromNdibData(ndib, developerId) : GetAppFromNdibData(ndib, developerId);
    
    // :||||||||||||||
    static Game GetGameFromNdibData(NdibData ndib, long developerId) => 
        new()
        {
            DeveloperId = developerId,
            Description = ndib.Description,
            Name = ndib.Name,
            Price = ndib.Price,
            Tags = string.Join(',', ndib.Tags),
            ImageCount = ndib.Images.Count,
            ContentClassification = ndib.GetContentClassification(),
            ProductType = ProductType.Game
        };

    static App GetAppFromNdibData(NdibData ndib, long developerId) => 
        new()
        {
            DeveloperId = developerId,
            Description = ndib.Description,
            Name = ndib.Name,
            Price = ndib.Price,
            Tags = string.Join(',', ndib.Tags),
            ImageCount = ndib.Images.Count,
            ProductType = ProductType.App
        };

    static Package GetPackageFromNdibData(NdibData ndib, Product product) => 
        new()
        {
            Product = product,
            Version = ndib.Version,
            WindowsExePath = ndib.WindowsExePath,
            LinuxExePath = ndib.LinuxExePath,
            MacExePath = ndib.MacExePath,
            MinRamMib = ndib.MinRamMib,
            MinCpu = ndib.MinCpu,
            MinDiskMib = ndib.MinDiskMib
        };
    
    public async Task<Stream> BuildUpdate(Package src, Package dst, Platform platform, CancellationToken ct)
    {
        var zipPath = PathsResolver.GetPatchResultZipPath(src, dst, platform);
        if (sftpClient.Exists(zipPath))
        {
            var tempPath = Path.GetTempFileName();
            sftpClient.DownloadFile(tempPath, zipPath);
            return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }
    
        var patchTempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        var zipTempPath = Path.GetTempFileName();
        Directory.CreateDirectory(patchTempPath);

        var srcPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        var dstPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        sftpClient.DownloadDirectory(srcPath, PathsResolver.GetVersionPath(src.ProductGuid, src.Guid));
        sftpClient.DownloadDirectory(dstPath, PathsResolver.GetVersionPath(dst.ProductGuid, dst.Guid));
        PatchBuilder.CreatePatch(srcPath, dstPath, patchTempPath, ct: ct);

        var srcPlatformSpecificPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        var dstPlatformSpecificPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        sftpClient.DownloadDirectory(srcPlatformSpecificPath, PathsResolver.GetVersionPath(src.ProductGuid, src.Guid, platform));
        sftpClient.DownloadDirectory(dstPlatformSpecificPath, PathsResolver.GetVersionPath(dst.ProductGuid, dst.Guid, platform));
        PatchBuilder.CreatePatch(srcPlatformSpecificPath, dstPlatformSpecificPath, patchTempPath, platform, ct);

        if (File.Exists(zipTempPath))
            File.Delete(zipTempPath);
        
        ZipFile.CreateFromDirectory(patchTempPath, zipTempPath);

        var memoryStream = new MemoryStream();
        using var fileStream = new FileStream(zipTempPath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        sftpClient.CreateDirectory(PathsResolver.GetPatchVersionPath(src, dst, platform));
        sftpClient.UploadFile(zipTempPath, zipPath);

        Directory.Delete(srcPath, true);
        Directory.Delete(dstPath, true);
        Directory.Delete(srcPlatformSpecificPath, true);
        Directory.Delete(dstPlatformSpecificPath, true);
        Directory.Delete(patchTempPath, true);
        File.Delete(zipTempPath);

        return memoryStream;
    }

    static Dictionary<string, string> GetFileHashes(string directoryPath)
    {
        var fileHashes = new Dictionary<string, string>();
        var filePaths = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        foreach (var path in filePaths)
        {
            var relativePath = GetRelativePath(directoryPath, path);
            fileHashes[relativePath] = ComputeFileHash(path);  
        } 

        return fileHashes;
    }

    static string GetRelativePath(string rootPath, string fullPath)
    {
        var rootDirLength = rootPath.Length + (rootPath.EndsWith("\\") ? 0 : 1);
        return fullPath[rootDirLength..];
    }

    static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public void ApplyNdibDataToProduct(Product product, NdibData ndibData)
    {
        product.Name = ndibData.Name;
        product.Description = ndibData.Description;
        product.Tags = string.Join(',', ndibData.Tags);
        product.Price = ndibData.Price;
        product.ImageCount = ndibData.Images.Count;
    }

    public void DownloadWholeProduct(MemoryStream stream, Guid productGuid, Guid packageGuid, Platform platform)
    {
        var remotePath = PathsResolver.GetWholeProductZipPath(productGuid, packageGuid, platform);
        if (sftpClient.Exists(remotePath))
            sftpClient.DownloadFile(stream, remotePath);
        else
        {
            var tempSourcesPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
            sftpClient.DownloadDirectory(tempSourcesPath, PathsResolver.GetVersionPath(productGuid, packageGuid));
            sftpClient.DownloadDirectory(tempSourcesPath, PathsResolver.GetVersionPath(productGuid, packageGuid, platform));
            ZipFile.CreateFromDirectory(tempSourcesPath, stream);
            stream.Position = 0;
            sftpClient.UploadStream(stream, remotePath);
            Directory.Delete(tempSourcesPath, true);
        }
    }
}