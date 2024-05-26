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

    [HttpGet("download/{srcGuid}/{dstGuid}")]
    public async Task<ActionResult> GetUpdate(Guid srcGuid, Guid dstGuid, CancellationToken ct)
    {
        var srcPackage = await packageRepo.GetById(srcGuid.Deobfuscate().Id, ct: ct);
        var dstPackage = await packageRepo.GetById(dstGuid.Deobfuscate().Id, ct: ct);
        if (srcPackage == null || dstPackage == null) return Problem();

        return File(await ndibService.BuildUpdate(srcPackage, dstPackage, ct), "application/zip", PathsResolver.RESULT_FILE);
    }


    [HttpGet("download/{srcGuid}/latest")]
    public async Task<ActionResult> GetUpdate(Guid srcGuid, CancellationToken ct)
    {
        var srcPackage = await packageRepo.GetById(srcGuid.Deobfuscate().Id, ct: ct);
        if (srcPackage == null) return Problem();

        var latestPackage = (await packageRepo.GetAll(restrict: x => x.ProductId == srcPackage.ProductGuid.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (latestPackage == null) return Problem();

        return File(await ndibService.BuildUpdate(srcPackage, latestPackage, ct), "application/zip", PathsResolver.RESULT_FILE);
    }

    [HttpGet("download/{productGuid}")]
    public async Task<ActionResult> GetWhole(Guid productGuid, CancellationToken ct)
    {
        var latestPackage = (await packageRepo.GetAll(restrict: x => x.ProductId == productGuid.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (latestPackage == null) return Problem();
        
        return File(ndibService.DownloadWholeProduct(productGuid, latestPackage.Guid), "application/zip", PathsResolver.RESULT_FILE);
    }

    [HttpPost("upload")]
    public async Task<ActionResult> UploadNew(IFormFile binFile, IFormFile metadataFile, CancellationToken ct)
    {
        if (binFile == null || metadataFile == null) return BadRequest("2 files expected");

        var developer = await ndibService.GetUserDeveloper(HttpContext.GetUserGuid(), ct);
        if (developer == null) return Unauthorized();

        var metadataTempPath = Guid.NewGuid().ToString();
        var ndibData = await ndibService.ExtractZipToTemp(metadataFile, metadataTempPath, ct);
        if (ndibData == null) return Problem();

        var binTempPath = Guid.NewGuid().ToString();
        await ndibService.ExtractZipToTemp(binFile, binTempPath, ct);
        
        var product = (await productRepo.GetAll(restrict: x => x.Name == ndibData.Name, expand: [x => x.Developer!], ct: ct)).FirstOrDefault();
        Package newPackage;
        if (product == null)
            newPackage = await ndibService.PersistPackage(ndibData, developer.Id, ct);
        else if (!ndibService.UserIsFromDeveloper(HttpContext.GetUserGuid(), product.Developer!))
            return Unauthorized();
        else
            newPackage = await ndibService.PersistPackage(ndibData, product, ct);

        await ndibService.UploadImagesToStorage(ndibData, metadataTempPath, newPackage.ProductGuid, ct);
        ndibService.SaveVersionFiles(binTempPath, newPackage);

        return Ok();
    }

    [HttpPost("update-metadata/{productGuid}")]
    public async Task<ActionResult> UpdateMetadata(Guid productGuid, IFormFile metadataFile, CancellationToken ct)
    {
        if (metadataFile == null || metadataFile.Length == 0) return BadRequest("No file uploaded.");

        var tempPath = Guid.NewGuid().ToString();
        var ndibData = await ndibService.ExtractZipToTemp(metadataFile, tempPath, ct);
        if (ndibData == null) return Problem();

        var product = await productRepo.GetById(productGuid.Deobfuscate().Id, [x => x.Developer!], ct);
        if (product == null) return Problem();
        if (!ndibService.UserIsFromDeveloper(HttpContext.GetUserGuid(), product.Developer!)) return Unauthorized();

        ndibService.ApplyNdibDataToProduct(product, ndibData);
        await productRepo.UpdateAsync(product, ct);
        await productRepo.CommitAsync(ct);
        await ndibService.UploadImagesToStorage(ndibData, tempPath, productGuid, ct);

        return Ok();
    }
}