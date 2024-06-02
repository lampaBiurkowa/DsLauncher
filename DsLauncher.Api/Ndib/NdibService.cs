using System.IO.Compression;
using DibBase.Infrastructure;
using DsLauncher.Api.Models;
using DsStorage.ApiClient;
using DsSftpLib;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

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
    readonly DsStorageClient storageClient = dsStorage.CreateClient(string.Empty); //:D/
    readonly FileExtensionContentTypeProvider contentTypeProvider = new();
    
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

            if (entry.FullName.Equals(metadataPath, StringComparison.OrdinalIgnoreCase))
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
                    entry.ExtractToFile(destinationPath, overwrite: false);
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
            await UploadFileToStorage(originalPath, $"{productGuid}", fileName, ct);
        }

        if (!string.IsNullOrWhiteSpace(ndibData.Background))
            await UploadFileToStorage(Path.Combine(tempPath, ndibData.Background), $"{productGuid}", $"bg{Path.GetExtension(ndibData.Background)}", ct);
        
        if (!string.IsNullOrWhiteSpace(ndibData.Icon))
            await UploadFileToStorage(Path.Combine(tempPath, ndibData.Icon), $"{productGuid}", $"icon{Path.GetExtension(ndibData.Icon)}", ct);
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
        var tempPath = Guid.NewGuid().ToString();
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
        // var dedicatedProduct = await GetDedicatedProduct(product, ct) ?? throw new Exception();
        ApplyNdibDataToProduct(product, ndib);
        await productRepo.UpdateAsync(product, ct);
        var newPackage = GetPackageFromNdibData(ndib, product);
        newPackage.Product = null;
        newPackage.ProductId = product.Id; //hzd ultra
        await packageRepo.InsertAsync(newPackage, ct);

        // if (ndib.IsGame)
        //     await AddAsGame(ndib, dedicatedProduct, ct);
        // else
        //     await AddAsApp(dedicatedProduct, ct);

        await packageRepo.CommitAsync(ct);

        return newPackage;
    }

    // async Task<Product?> GetDedicatedProduct(Product product, CancellationToken ct)
    // {
    //     if (product.ProductType == ProductType.Game)
    //         return await gameRepo.GetById(product.Id, ct: ct);
    //     else if (product.ProductType == ProductType.App)
    //         return await appRepo.GetById(product.Id, ct: ct);
    //     else
    //         return null;
    // }

    public async Task AddAsGame(NdibData ndib, Product product, CancellationToken ct)
    {
        var game = (Game)product;
        game.ProductType = ProductType.Game;
        game.ContentClassification = ndib.GetContentClassification();
        await gameRepo.InsertAsync(game, ct);
    }

    public async Task AddAsApp(Product product, CancellationToken ct)
    {
        var app = (App)product;
        app.ProductType = ProductType.App;
        await appRepo.InsertAsync(app, ct);
    }

    public async Task<Developer?> GetUserDeveloper(Guid? userGuid, CancellationToken ct)
    {
        if (userGuid == null) return null;
        return (await developerRepo.GetAll(ct: ct)).FirstOrDefault(x => x.UserGuids.Contains((Guid)userGuid));
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

    async Task UploadFileToStorage(string srcPath, string bucketName, string remoteName, CancellationToken ct)
    {
        using var stream = File.OpenRead(srcPath);
        contentTypeProvider.TryGetContentType(srcPath, out var contentType);
        await storageClient.Storage_UploadFileToBucketAsync(bucketName, new(stream, remoteName, contentType), ct);
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
            ImageCount = ndib.Images.Count   
        };

    static App GetAppFromNdibData(NdibData ndib, long developerId) => 
        new()
        {
            DeveloperId = developerId,
            Description = ndib.Description,
            Name = ndib.Name,
            Price = ndib.Price,
            Tags = string.Join(',', ndib.Tags),
            ImageCount = ndib.Images.Count   
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
    
        var patchTempPath = $"{Guid.NewGuid()}";
        var zipTempPath = $"{Guid.NewGuid()}.zip";
        Directory.CreateDirectory(patchTempPath);

        var srcPath = PathsResolver.GetVersionPath(src.ProductGuid, src.Guid);
        var dstPath = PathsResolver.GetVersionPath(dst.ProductGuid, dst.Guid);
        sftpClient.DownloadDirectory(srcPath, srcPath);
        sftpClient.DownloadDirectory(dstPath, dstPath);
        await PatchBuilder.CreatePatch(src, dst, patchTempPath, ct: ct);

        var srcPlatformSpecificPath = PathsResolver.GetVersionPath(src.ProductGuid, src.Guid, platform);
        var dstPlatformSpecificPath = PathsResolver.GetVersionPath(dst.ProductGuid, dst.Guid, platform);
        sftpClient.DownloadDirectory(srcPlatformSpecificPath, srcPlatformSpecificPath);
        sftpClient.DownloadDirectory(dstPlatformSpecificPath, dstPlatformSpecificPath);
        await PatchBuilder.CreatePatch(src, dst, patchTempPath, platform, ct);

        if (File.Exists(zipTempPath))
            File.Delete(zipTempPath);
        
        ZipFile.CreateFromDirectory(patchTempPath, zipTempPath);
        // var bytes = File.ReadAllBytes(zipTempPath);
        // sftpClient.CreateDirectory(PathsResolver.GetPatchVersionPath(src, dst, platform));
        // sftpClient.UploadFile(zipTempPath, zipPath);
        // Directory.Delete(srcPath, true);
        // Directory.Delete(dstPath, true);
        // Directory.Delete(patchTempPath, true);
        // File.Delete(zipTempPath);

        // return bytes;

        var memoryStream = new MemoryStream();
        using var fileStream = new FileStream(zipTempPath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        sftpClient.CreateDirectory(PathsResolver.GetPatchVersionPath(src, dst, platform));
        sftpClient.UploadFile(zipTempPath, zipPath);

        Directory.Delete(srcPath, true);
        Directory.Delete(dstPath, true);
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

    public FileStream DownloadWholeProduct(Guid productGuid, Guid packageGuid, Platform platform)
    {
        var tempZipPath = $"{Guid.NewGuid()}.zip";
        var remotePath = PathsResolver.GetWholeProductZipPath(productGuid, packageGuid, platform);
        if (sftpClient.Exists(remotePath))
            sftpClient.DownloadFile(tempZipPath, remotePath);
        else
        {
            var tempSourcesPath = $"{Guid.NewGuid()}";
            sftpClient.DownloadDirectory(tempSourcesPath, PathsResolver.GetVersionPath(productGuid, packageGuid));
            sftpClient.DownloadDirectory(tempSourcesPath, PathsResolver.GetVersionPath(productGuid, packageGuid, platform));
            ZipFile.CreateFromDirectory(tempSourcesPath, tempZipPath);
            sftpClient.UploadFile(tempZipPath, remotePath);
            Directory.Delete(tempSourcesPath, true);
        }

        var stream = File.OpenRead(tempZipPath);
        File.Delete(tempZipPath);
        return stream;
    }
}