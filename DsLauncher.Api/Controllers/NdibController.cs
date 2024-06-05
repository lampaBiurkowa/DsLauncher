using DibBase.Extensions;
using DibBase.Infrastructure;
using DsCore.ApiClient;
using DsLauncher.Api.Ndib;
using DsLauncher.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DsLauncher.Api;

[ApiController]
[Authorize]
[Route("[controller]")]
public class NdibController(NdibService ndibService, Repository<Package> packageRepo, Repository<Product> productRepo) : ControllerBase
{
    const string LATEST_PACKAGE_GUID_HEADER = "Latest-Package";

    [HttpGet("download/{srcGuid}/{dstGuid}/{platform}")]
    public async Task<ActionResult> GetUpdate(Guid srcGuid, Guid dstGuid, Platform platform, CancellationToken ct)
    {
        var srcPackage = await packageRepo.GetById(srcGuid.Deobfuscate().Id, ct: ct);
        var dstPackage = await packageRepo.GetById(dstGuid.Deobfuscate().Id, ct: ct);
        if (srcPackage == null || dstPackage == null) return Problem();

        return File(await ndibService.BuildUpdate(srcPackage, dstPackage, platform, ct), "application/zip", PathsResolver.RESULT_FILE);
    }


    [HttpGet("download/{srcGuid}/latest/{platform}")]
    public async Task<ActionResult> GetUpdateToLatest(Guid srcGuid, Platform platform, CancellationToken ct)
    {
        var srcPackage = await packageRepo.GetById(srcGuid.Deobfuscate().Id, ct: ct);
        if (srcPackage == null) return Problem();

        var latestPackage = (await packageRepo.GetAll(restrict: x => x.ProductId == srcPackage.ProductGuid.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (latestPackage == null) return Problem();

        HttpContext.Response.Headers.Append(LATEST_PACKAGE_GUID_HEADER, latestPackage.Guid.ToString());
        return File(await ndibService.BuildUpdate(srcPackage, latestPackage, platform, ct), "application/zip", PathsResolver.RESULT_FILE);
    }

    [HttpGet("download/whole/{productGuid}/{platform}")]
    public async Task<ActionResult> GetWhole(Guid productGuid, Platform platform, CancellationToken ct)
    {
        var latestPackage = (await packageRepo.GetAll(restrict: x => x.ProductId == productGuid.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (latestPackage == null) return Problem();
        
        HttpContext.Response.Headers.Append(LATEST_PACKAGE_GUID_HEADER, latestPackage.Guid.ToString());
        
        return File(ndibService.DownloadWholeProduct(productGuid, latestPackage.Guid, platform), "application/zip", PathsResolver.RESULT_FILE);
    }

    [HttpGet("download/whole/{productGuid}/{platform}/{packageGuid}")]
    public ActionResult GetWholeVersion(Guid productGuid, Platform platform, Guid packageGuid) =>
        File(ndibService.DownloadWholeProduct(productGuid, packageGuid, platform), "application/zip", PathsResolver.RESULT_FILE);

    [DisableRequestSizeLimit]
    [HttpPost("upload")]
    public async Task<ActionResult> UploadNew(
        IFormFile coreFile,
        IFormFile metadataFile,
        IFormFile? winFile = null,
        IFormFile? linuxFile = null,
        IFormFile? macFile = null,
        CancellationToken ct = default)
    {
        if (coreFile == null || metadataFile == null) return BadRequest("core & metadata expected");

        var developer = await ndibService.GetUserDeveloper(HttpContext.GetUserGuid(), ct);
        if (developer == null) return Unauthorized();

        var metadataTempPath = Guid.NewGuid().ToString();
        var ndibData = await ndibService.ExtractZipToTemp(metadataFile, metadataTempPath, ct);
        if (ndibData == null) return Problem();
        
        var product = (await productRepo.GetAll(restrict: x => x.Name == ndibData.Name, expand: [x => x.Developer!], ct: ct)).FirstOrDefault();
        Package newPackage;
        if (product == null)
            newPackage = await ndibService.PersistPackage(ndibData, developer.Id, ct);
        else if (!ndibService.UserIsFromDeveloper(HttpContext.GetUserGuid(), product.Developer!))
            return Unauthorized();
        else
            newPackage = await ndibService.PersistPackage(ndibData, product, ct);

        await ndibService.UploadImagesToStorage(ndibData, metadataTempPath, newPackage.ProductGuid, ct);

        await ndibService.UploadVersion(coreFile, newPackage, ct: ct);
        if (winFile != null) await ndibService.UploadVersion(winFile, newPackage, Platform.win, ct);
        if (linuxFile != null) await ndibService.UploadVersion(linuxFile, newPackage, Platform.linux, ct);
        if (macFile != null) await ndibService.UploadVersion(macFile, newPackage, Platform.mac, ct);

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

    [HttpGet("version-hash/{packageGuid}/{platform}")]
    public async Task<ActionResult<Dictionary<string, string>>> GetVerificationHash(Guid packageGuid, Platform platform, CancellationToken ct)
    {
        var package = await packageRepo.GetById(packageGuid.Deobfuscate().Id, ct: ct);
        if (package == null) return Problem();

        return Ok(await ndibService.GetVersionVerificationHash(package, platform, ct));
    }
}