using DibBase.Extensions;
using DsLauncher.Models;

namespace DsLauncher.Api.Ndib;

public static class PathsResolver
{
    const string NDIB_PATH = "ndib";
    const string PATCH_PATH = "patch";
    public const string RESULT_FILE = "result.zip";
    public const string HASH_FILE = "hash.json";
    public const string DELETED_FILES_FILE = "deleted.txt";
    public const string WINDOWS_FOLDER_NAME = "win";
    public const string LINUX_FOLDER_NAME = "linux";
    public const string MAC_FOLDER_NAME = "mac";

    public static string GetVersionPath(Guid productGuid, Guid packageGuid) => $"{NDIB_PATH}/{productGuid}/{packageGuid}";
    public static string GetVersionHash(Package package) => $"{NDIB_PATH}/{package.ProductGuid}/{package.Obfuscate()}-{HASH_FILE}";
    public static string GetPatchVersionPath(Package srcPackage, Package dstPackage) =>
        $"{NDIB_PATH}/{srcPackage.ProductGuid}/{srcPackage.Obfuscate()}-{dstPackage.Obfuscate()}";
    public static string GetPatchDirectoryPath(Guid productGuid) => $"{NDIB_PATH}/{productGuid}/{PATCH_PATH}";
    public static string GetPatchResultZipPath(Package srcPackage, Package dstPackage) =>
        $"{GetPatchVersionPath(srcPackage, dstPackage)}-{RESULT_FILE}";
    public static string GetWholeProductZipPath(Guid productGuid, Guid packageGuid) => $"{NDIB_PATH}/{productGuid}/{packageGuid}.zip";
}