using System.IO.Compression;
using DibBase.Infrastructure;
using DsLauncher.Models;
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
    Repository<Developer> developerRepo,
    DsStorageClientFactory dsStorage,
    IDsSftpClient sftpClient)
{
    readonly Repository<Package> packageRepo = packageRepo;
    readonly Repository<Product> productRepo = productRepo;
    readonly Repository<Developer> developerRepo = developerRepo;
    readonly DsStorageClient storageClient = dsStorage.CreateClient(string.Empty); //:D/
    readonly FileExtensionContentTypeProvider contentTypeProvider = new();
    readonly IDsSftpClient sftpClient = sftpClient;
    
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
        Directory.Delete(tempPath, true);
    }

    public void SaveVersionFiles(string tempPath, Package newPackage)
    {   
        sftpClient.UploadDirectory(tempPath, PathsResolver.GetVersionPath(newPackage.ProductGuid, newPackage.Guid));
        var hash = JsonConvert.SerializeObject(GetFileHashes(tempPath));
        sftpClient.UploadStream(GenerateStreamFromString(hash), PathsResolver.GetVersionHash(newPackage));
        Directory.Delete(tempPath, true);
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
        ApplyNdibDataToProduct(product, ndib);
        await productRepo.UpdateAsync(product, ct);
        var newPackage = GetPackageFromNdibData(ndib, product);
        newPackage.Product = null;
        newPackage.ProductId = product.Id; //hzd ultra
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
    }

    static Product GetProductFromNdibData(NdibData ndib, long developerId, long productId = default) => 
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

    
    public async Task<byte[]> BuildUpdate(Package src, Package dst, CancellationToken ct)
    {
        var zipPath = PathsResolver.GetPatchResultZipPath(src, dst);
        if (sftpClient.Exists(zipPath))
        {
            var tempPath = Path.GetTempFileName();
            sftpClient.DownloadFile(tempPath, zipPath);
            return await File.ReadAllBytesAsync(tempPath, ct);
        }

        var srcPath = PathsResolver.GetVersionPath(src.ProductGuid, src.Guid);
        var dstPath = PathsResolver.GetVersionPath(dst.ProductGuid, dst.Guid);
        sftpClient.DownloadDirectory(srcPath, srcPath);
        sftpClient.DownloadDirectory(dstPath, dstPath);
    
        var patchTempPath = $"{Guid.NewGuid()}";
        var zipTempPath = $"{Guid.NewGuid()}.zip";
        Directory.CreateDirectory(patchTempPath);
        await PatchBuilder.CreatePatch(src, dst, patchTempPath, ct);

        if (File.Exists(zipTempPath))
            File.Delete(zipTempPath);
        
        ZipFile.CreateFromDirectory(patchTempPath, zipTempPath);
        var bytes = File.ReadAllBytes(zipTempPath);
        sftpClient.CreateDirectory(PathsResolver.GetPatchVersionPath(src, dst));
        sftpClient.UploadFile(zipTempPath, zipPath);
        Directory.Delete(srcPath, true);
        Directory.Delete(dstPath, true);
        Directory.Delete(patchTempPath, true);
        File.Delete(zipTempPath);

        return bytes;
    }

    static Dictionary<string, string> GetFileHashes(string directoryPath)
    {
        var fileHashes = new Dictionary<string, string>();
        var filePaths = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        foreach (var path in filePaths)
            fileHashes[path] = ComputeFileHash(path);

        return fileHashes;
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

    public FileStream DownloadWholeProduct(Guid productGuid, Guid packageGuid)
    {
        var tempZipPath = $"{Guid.NewGuid()}.zip";
        var remotePath = PathsResolver.GetWholeProductZipPath(productGuid, packageGuid);
        if (sftpClient.Exists(remotePath))
            sftpClient.DownloadFile(tempZipPath, remotePath);
        else
        {
            var tempSourcesPath = $"{Guid.NewGuid()}";
            sftpClient.DownloadDirectory(tempSourcesPath, PathsResolver.GetVersionPath(productGuid, packageGuid));
            ZipFile.CreateFromDirectory(tempSourcesPath, tempZipPath);
            sftpClient.UploadFile(tempZipPath, remotePath);
            Directory.Delete(tempSourcesPath, true);
        }

        var stream = File.OpenRead(tempZipPath);
        File.Delete(tempZipPath);
        return stream;
    }
}