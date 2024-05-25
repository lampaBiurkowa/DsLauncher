using System.IO.Compression;
using DsLauncher.Models;

namespace DsLauncher.Api.Ndib;

public static class UpdateBuilder
{
    public static byte[] BuildUpdate(Package src, Package dst)
    {
        Directory.CreateDirectory(PathsResolver.GetPatchDirectoryPath(src.ProductGuid));
        PatchBuilder.CreatePatches(src, dst);

        var zipPath = PathsResolver.GetPatchResultZipPath(src, dst);
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        
        ZipFile.CreateFromDirectory(PathsResolver.GetPatchVersionPath(src, dst), zipPath);
        return File.ReadAllBytes(zipPath);
    }
}