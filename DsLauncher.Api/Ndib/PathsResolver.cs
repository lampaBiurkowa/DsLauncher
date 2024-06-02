using DibBase.Extensions;
using DsLauncher.Api.Models;

namespace DsLauncher.Api.Ndib;

public enum Platform { win, linux, mac }

public static class PathsResolver
{
    const string NDIB_PATH = "ndib";
    const string PATCH_PATH = "patch";
    public const string RESULT_FILE = "result.zip";
    public const string HASH_FILE = "hash.json";
    // public const string DELETED_FILES_FILE = "deleted.dsdel";

    public static string GetVersionPath(Guid productGuid, Guid packageGuid, Platform? platform = null) =>
        $"{NDIB_PATH}/{productGuid}/{packageGuid}{(platform != null ? $"-{platform}" : "")}";
    public static string GetVersionVerificationHash(Package package, Platform? platform = null) =>
        $"{NDIB_PATH}/{package.ProductGuid}/{package.Obfuscate()}{(platform != null ? $"-{platform}" : "")}-{HASH_FILE}";
    public static string GetPatchVersionPath(Package srcPackage, Package dstPackage, Platform platform) =>
        $"{NDIB_PATH}/{srcPackage.ProductGuid}/{srcPackage.Obfuscate()}-{dstPackage.Obfuscate()}-{platform}";
    public static string GetPatchDirectoryPath(Guid productGuid) => $"{NDIB_PATH}/{productGuid}/{PATCH_PATH}";
    public static string GetPatchResultZipPath(Package srcPackage, Package dstPackage, Platform platform) =>
        $"{GetPatchVersionPath(srcPackage, dstPackage, platform)}-{RESULT_FILE}";
    public static string GetWholeProductZipPath(Guid productGuid, Guid packageGuid, Platform platform) =>
        $"{NDIB_PATH}/{productGuid}/{packageGuid}-{platform}.zip";
}