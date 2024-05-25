using DiffPlex.DiffBuilder;
using DsLauncher.Models;
using Newtonsoft.Json;

namespace DsLauncher.Api.Ndib;

public static class PatchBuilder
{
    public static void CreatePatches(Package srcPackage, Package dstPackage)
    {
        var srcVerPath = PathsResolver.GetVersionPath(srcPackage);
        var dstVerPath = PathsResolver.GetVersionPath(dstPackage);
        var patchPath = PathsResolver.GetPatchVersionPath(srcPackage, dstPackage);
        var srcFiles = Directory.GetFiles(srcVerPath, "*", SearchOption.AllDirectories);
        var dstFiles = Directory.GetFiles(dstVerPath, "*", SearchOption.AllDirectories);

        var differ = new InlineDiffBuilder(new DiffPlex.Differ());

        foreach (var file in dstFiles)
        {
            var relativePath = Path.GetRelativePath(dstVerPath, file);
            var correspondingFileInV1 = Path.Combine(srcVerPath, relativePath);
            var patchFilePath = Path.Combine(patchPath, relativePath + ".patch");
            
            var missingDirectories = Path.GetDirectoryName(patchFilePath);
            if (missingDirectories != null)
                Directory.CreateDirectory(missingDirectories);

            if (File.Exists(correspondingFileInV1))
            {
                var oldText = File.ReadAllText(correspondingFileInV1);
                var newText = File.ReadAllText(file);

                var diff = differ.BuildDiffModel(oldText, newText);
                using var writer = new StreamWriter(patchFilePath);
                foreach (var line in diff.Lines)
                    writer.WriteLine($"{line.Type}:{line.Text}");
            }
            else
                File.Copy(file, patchFilePath.Replace(".patch", ""), true);
        }

        var deletedFiles = srcFiles.Except(dstFiles).ToList();
        var deletedFilesPath = Path.Combine(patchPath, "deleted_files.json");
        File.WriteAllText(deletedFilesPath, JsonConvert.SerializeObject(deletedFiles));
    }
}