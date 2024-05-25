using DibBase.Extensions;
using DsLauncher.Models;

namespace DsLauncher.Api.Ndib;

public static class PathsResolver
{
    const string NDIB_PATH = "ndib";
    const string PATCH_PATH = "patch";
    public const string RESULT_FILE = "result.zip";

    public static string GetVersionPath(Package package) => $"{NDIB_PATH}/{package.ProductGuid}/{package.Obfuscate()}";
    public static string GetPatchVersionPath(Package srcPackage, Package dstPackage) =>
        $"{NDIB_PATH}/{srcPackage.ProductGuid}/{PATCH_PATH}/{srcPackage.Obfuscate()}-{dstPackage.Obfuscate()}";
    public static string GetPatchDirectoryPath(Guid productGuid) => $"{NDIB_PATH}/{productGuid}/{PATCH_PATH}";
    public static string GetPatchResultZipPath(Package srcPackage, Package dstPackage) =>
        $"{NDIB_PATH}/{srcPackage.ProductGuid}/{PATCH_PATH}/{srcPackage.Obfuscate()}-{dstPackage.Obfuscate()}/{RESULT_FILE}";
}