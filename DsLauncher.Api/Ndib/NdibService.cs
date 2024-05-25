using System.IO.Compression;
using DibBase.Infrastructure;
using DsLauncher.Models;
using DsStorage.ApiClient;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;

namespace DsLauncher.Api.Ndib;

public class NdibService(
    Repository<Package> packageRepo,
    Repository<Product> productRepo,
    Repository<Developer> developerRepo,
    DsStorageClientFactory dsStorage)
{
    readonly Repository<Package> packageRepo = packageRepo;
    readonly Repository<Product> productRepo = productRepo;
    readonly Repository<Developer> developerRepo = developerRepo;
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

        await UploadFileToStorage(Path.Combine(tempPath, ndibData.Background), $"{productGuid}", $"bg{Path.GetExtension(ndibData.Background)}", ct);
        await UploadFileToStorage(Path.Combine(tempPath, ndibData.Icon), $"{productGuid}", $"icon{Path.GetExtension(ndibData.Icon)}", ct);
        
    }

    public void SaveVersionFiles(string tempPath, Package newPackage)
    {
        var dstPath = PathsResolver.GetVersionPath(newPackage);
        var directory = Directory.GetParent(dstPath);
        if (directory != null && !Directory.Exists(directory.FullName))
            Directory.CreateDirectory(directory.FullName);
        
        Directory.Move(tempPath, dstPath);
    }

    public async Task<Package> PersistPackage(NdibData ndib, long developerId, long productId = default, CancellationToken ct = default)
    {
        var newProduct = GetProductFromNdibData(ndib, developerId, productId);
        var newPackage = GetPackageFromNdibData(ndib, newProduct);
        if (productId != default)
        {
            newPackage.ProductId = productId;
            newPackage.Product = null;
            await productRepo.UpdateAsync(newProduct, ct);
        }
        await packageRepo.InsertAsync(newPackage, ct);
        await packageRepo.CommitAsync(ct);

        return newPackage;
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

    async Task UploadFileToStorage(string srcPath, string bucketName, string remoteName, CancellationToken ct)
    {
        using var stream = File.OpenRead(srcPath);
        contentTypeProvider.TryGetContentType(srcPath, out var contentType);
        await storageClient.Storage_UploadFileToBucketAsync(bucketName, new(stream, remoteName, contentType), ct);
        File.Delete(srcPath);
    }

    static Product GetProductFromNdibData(NdibData ndib, long developerId, long productId) => 
        new()
        {
            Id = productId,
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
            ExePath = ndib.ExePath,
            Version = ndib.Version,
            IsWin = ndib.Windows,
            IsLinux = ndib.Linux,
            IsMac = ndib.Mac,
            RamMib = ndib.RamMib,
            CpuMhz = ndib.CpuMhz,
            DiskMib = ndib.DiskMib
        };
}