using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseApi;
using DibBaseSampleApi.Controllers;
using DsCore.ApiClient;
using DsLauncher.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class PackageController(Repository<Package> repository, AccessContext context) : EntityController<Package>(repository)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Package entity, CancellationToken ct)
    {
        if (!await BelongsToDeveloper(entity.Guid, ct)) return Unauthorized();

        return await base.Add(entity, ct);
    }

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Package entity, CancellationToken ct)
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

    [HttpGet("latest/{productId}")]
    public async Task<ActionResult<Package?>> GetLatest(Guid productId, CancellationToken ct)
    {
        var result = (await repo.GetAll(restrict: x => x.ProductId == productId.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (result == null) return Ok(null);

        IdHelper.HidePrivateId(result);
        return Ok(result);
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<List<Package>>> GetForProduct(Guid productId, CancellationToken ct)
    {
        var result = (await repo.GetAll(restrict: x => x.ProductId == productId.Deobfuscate().Id, ct: ct))
            .Select(IdHelper.HidePrivateId);

        return Ok(result);
    }

    async Task<bool> BelongsToDeveloper(Guid packageGuid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return false;

        var item = await repo.GetById(packageGuid.Deobfuscate().Id, [x => x.Product!], ct);
        if (item == null) return false;
        repo.Clear();

        return await context.BelongsToDeveloper((Guid)userGuid, item.Product!.DeveloperGuid, ct);
    }
}