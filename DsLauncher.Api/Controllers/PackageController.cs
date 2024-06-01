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
public class PackageController(Repository<Package> repository) : EntityController<Package>(repository)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Package entity, CancellationToken ct) => await base.Add(entity, ct);

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Package entity, CancellationToken ct)
    {
        var item = await repo.GetById(entity.Guid.Deobfuscate().Id, [x => x.Product], ct);
        if (item == null) return Problem();
        if (!HttpContext.IsUser(item.Product.DeveloperGuid)) return Unauthorized();
        return await base.Update(entity, ct);
    }

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await repo.GetById(id.Deobfuscate().Id, [x => x.Product], ct);
        if (item == null) return Problem();
        if (!HttpContext.IsUser(item.Product.DeveloperGuid)) return Unauthorized();
        
        await base.Delete(id, ct);
        return Ok();
    }

    [HttpGet("latest/{productId}")]
    public async Task<ActionResult<Package?>> GetLatest(Guid productId, CancellationToken ct)
    {
        var result = (await repo.GetAll(restrict: x => x.ProductId == productId.Deobfuscate().Id, ct: ct)).MaxBy(x => x.CreatedAt);
        if (result == null) return Ok(null);

        IdHelper.HidePrivateId(result);
        return Ok(result);
    }
}