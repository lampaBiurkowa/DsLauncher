using DibBase.Extensions;
using DibBase.Infrastructure;
using DsIdentity.ApiClient;
using DsLauncher.Api.Ndib;
using DsLauncher.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class NdibController(NdibService ndibService, Repository<Package> packageRepo, Repository<Product> productRepo) : ControllerBase
{
    readonly NdibService ndibService = ndibService;
    readonly Repository<Package> packageRepo = packageRepo;
    readonly Repository<Product> productRepo = productRepo;

    [HttpGet("download/{srcGuid}/{dstguid}")]
    public async Task<ActionResult> GetUpdate(Guid srcGuid, Guid dstGuid, CancellationToken ct)
    {
        var srcPackage = await packageRepo.GetById(srcGuid.Deobfuscate().Id, ct: ct);
        var dstPackage = await packageRepo.GetById(dstGuid.Deobfuscate().Id, ct: ct);
        if (srcPackage == null || dstPackage == null) return Problem();

        return File(UpdateBuilder.BuildUpdate(srcPackage, dstPackage), "application/zip", PathsResolver.RESULT_FILE);
    }


    [HttpGet("download/{srcGuid}/latest")]
    public async Task<ActionResult> GetUpdate(Guid srcGuid, CancellationToken ct)
    {
        var srcPackage = await packageRepo.GetById(srcGuid.Deobfuscate().Id, ct: ct);
        if (srcPackage == null) return Problem();

        var latestPackage = (await packageRepo.GetAll(restrict: x => x.ProductId == srcPackage.ProductGuid.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (latestPackage == null) return Problem();

        return File(UpdateBuilder.BuildUpdate(srcPackage, latestPackage), "application/zip", PathsResolver.RESULT_FILE);
    }

    [HttpGet("download/{productGuid}")]
    public async Task<ActionResult> GetWhole(Guid productGuid, CancellationToken ct)
    {
        var latestPackage = (await packageRepo.GetAll(restrict: x => x.ProductId == productGuid.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (latestPackage == null) return Problem();
        
        var zipPath = $"{PathsResolver.GetVersionPath(latestPackage)}.zip";
        ZipFile.CreateFromDirectory(PathsResolver.GetVersionPath(latestPackage), zipPath);
        return File(System.IO.File.OpenRead(zipPath), "application/zip", PathsResolver.RESULT_FILE);
    }

    [HttpPost("upload")]
    public async Task<ActionResult> UploadNew(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        var developer = await ndibService.GetUserDeveloper(HttpContext.GetUserGuid(), ct);
        if (developer == null) return Unauthorized();

        var temporaryPath = Guid.NewGuid().ToString();
        var ndibData = await ndibService.ExtractZipToTemp(file, temporaryPath, ct);
        if (ndibData == null) return Problem();

        var newPackage = await ndibService.PersistPackage(ndibData, developer.Id, ct: ct);
        await ndibService.UploadImagesToStorage(ndibData, temporaryPath, newPackage.ProductGuid, ct);
        ndibService.SaveVersionFiles(temporaryPath, newPackage);

        return Ok();
    }

    [HttpPost("upload/{productGuid}")]
    public async Task<ActionResult> UploadUpdate(Guid productGuid, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        var product = await productRepo.GetById(productGuid.Deobfuscate().Id, expand: [x => x.Developer!], ct: ct);
        if (product == null) return Problem();
        if (!ndibService.UserIsFromDeveloper(HttpContext.GetUserGuid(), product.Developer!)) return Unauthorized();

        var temporaryPath = Guid.NewGuid().ToString();
        var ndibData = await ndibService.ExtractZipToTemp(file, temporaryPath, ct);
        if (ndibData == null) return Problem();

        var newPackage = await ndibService.PersistPackage(ndibData, product.Developer!.Id, product.Id, ct);
        await ndibService.UploadImagesToStorage(ndibData, temporaryPath, newPackage.ProductGuid, ct);
        ndibService.SaveVersionFiles(temporaryPath, newPackage);

        return Ok();
    }
}