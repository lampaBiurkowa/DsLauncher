using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsCore.ApiClient;
using DsLauncher.Api.Models;
using DsStorage.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class DeveloperController(
    Repository<Developer> repository,
    DsStorageClientFactory dsStorage,
    AccessContext context) : EntityController<Developer>(repository)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Developer entity, CancellationToken ct) => await base.Add(entity, ct);

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Developer entity, CancellationToken ct)
    {
        if (!await BelongsToDeveloper(entity.Guid, ct)) return Unauthorized();
        return await base.Update(entity, ct);
    }

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid guid, CancellationToken ct)
    {
        if (!await BelongsToDeveloper(guid, ct)) return Unauthorized();

        return await base.Delete(guid, ct);
    }

    [HttpGet("user/{userGuid}")]
    public async Task<ActionResult<Developer?>> GetByUser(Guid userGuid, CancellationToken ct) =>
        (await repo.GetAll(ct: ct)).FirstOrDefault(x => x.UserGuids.Contains(userGuid));

    [HttpPost]
    [Authorize]
    [Route("{guid}/UploadProfileImage")]
    public async Task<ActionResult<string>> UploadProfileImage(Guid guid, IFormFile file, CancellationToken ct)
    {
        if (!await BelongsToDeveloper(guid, ct)) return Unauthorized();
        
        var developer = await repo.GetById(guid.Deobfuscate().Id, ct: ct);
        if (developer == null) return Unauthorized();
        
        var client = dsStorage.CreateClient();
        var filename = await client.Storage_UploadFileToDefaultBucketAsync(new DsStorage.ApiClient.FileParameter(file.OpenReadStream(), file.FileName, file.ContentType), ct);
        
        developer.ProfileImage = filename;
        await repo.UpdateAsync(developer, ct);
        await repo.CommitAsync(ct);

        return Ok(filename);
    }

    async Task<bool> BelongsToDeveloper(Guid developerGuid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return false;

        var item = await repo.GetById(developerGuid.Deobfuscate().Id, ct: ct);
        if (item == null) return false;
        repo.Clear();

        return await context.BelongsToDeveloper((Guid)userGuid, item.Guid, ct);
    }
}