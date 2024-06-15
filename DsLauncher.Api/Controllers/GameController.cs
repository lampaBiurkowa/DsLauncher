using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsCore.ApiClient;
using DsLauncher.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class GameController(Repository<Game> repo, Repository<Purchase> purchaseRepo) : EntityController<Game>(repo)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Game entity, CancellationToken ct) => await base.Add(entity, ct);

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Game entity, CancellationToken ct)
    {
        if (!HttpContext.IsUser(entity.DeveloperGuid)) return Unauthorized();
        return await base.Update(entity, ct);
    }

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await repo.GetById(id.Deobfuscate().Id, ct: ct);
        if (item == null) return Problem();
        if (!HttpContext.IsUser(item.DeveloperGuid)) return Unauthorized();

        return await base.Delete(id, ct);
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<ActionResult<List<Guid>>> GetByUser(CancellationToken ct = default)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var purchases = await purchaseRepo.GetAll(restrict: x => x.UserGuid == userGuid, ct: ct);
        var gamePurchases = await repo.GetByIds(purchases.Select(x => x.Id), ct: ct);
        return Ok(gamePurchases.Select(x => x.Guid));
    }

    [HttpGet("ids")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetIds(int skip = 0, int take = 0, CancellationToken ct = default)
        => Ok((await repo.GetAll(skip, take, ct: ct)).Select(x => x.Guid));
}