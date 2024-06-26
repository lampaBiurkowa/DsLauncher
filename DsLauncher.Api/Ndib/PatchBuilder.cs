using System.Security.Cryptography;

namespace DsLauncher.Api.Ndib;

public static class PatchBuilder
{
    public static void CreatePatch(string srcVerPath, string dstVerPath, string patchPath, Platform? platform = null, CancellationToken ct = default)
    {
        var srcFiles = GetFileHashes(srcVerPath);
        var dstFiles = GetFileHashes(dstVerPath);

        var modifiedFiles = new List<string>();
        var newFilesList = new List<string>();
        var deletedFiles = new List<string>();

        foreach (var oldFile in srcFiles)
        {
            if (dstFiles.TryGetValue(oldFile.Key, out var newFileHash))
            {
                if (oldFile.Value != newFileHash)
                    modifiedFiles.Add(oldFile.Key);

                dstFiles.Remove(oldFile.Key);
            }
            else
                deletedFiles.Add(oldFile.Key);
        }

        newFilesList.AddRange(dstFiles.Keys);
        Directory.CreateDirectory(patchPath);

        foreach (var file in modifiedFiles.Concat(newFilesList))
        {
            var srcPath = Path.Combine(dstVerPath, file);
            var dstPath = Path.Combine(patchPath, file);

            var parentDir = Path.GetDirectoryName(dstPath);
            if (parentDir != null) Directory.CreateDirectory(parentDir);

            File.Copy(srcPath, dstPath, true);
        }
    }

    static Dictionary<string, string> GetFileHashes(string directory)
    {
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        var fileHashes = new Dictionary<string, string>();

        using var sha256 = SHA256.Create();
        foreach (var file in files)
        {
            var relativePath = file.Substring(directory.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
            using var stream = File.OpenRead(file);
            var hash = sha256.ComputeHash(stream);
            fileHashes[relativePath] = BitConverter.ToString(hash).Replace("-", "");
        }

        return fileHashes;
    }
}