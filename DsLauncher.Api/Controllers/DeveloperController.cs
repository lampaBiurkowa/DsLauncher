using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsIdentity.ApiClient;
using DsLauncher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class DeveloperController(Repository<Developer> repository) : EntityController<Developer>(repository)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Developer entity, CancellationToken ct) => await base.Add(entity, ct);

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Developer entity, CancellationToken ct)
    {
        if (!HttpContext.IsUser(entity.Guid)) return Unauthorized();
        return await base.Update(entity, ct);
    }

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await repo.GetById(id.Deobfuscate().Id, ct: ct);
        if (item == null) return Problem();
        if (!HttpContext.IsUser(item.Guid)) return Unauthorized();

        return await base.Delete(id, ct);
    }

    [HttpGet("user/{userGuid}")]
    public async Task<ActionResult<Developer?>> GetByUser(Guid userGuid, CancellationToken ct) =>
        (await repo.GetAll(ct: ct)).FirstOrDefault(x => x.UserGuids.Contains(userGuid));
}