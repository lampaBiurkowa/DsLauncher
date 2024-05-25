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
public class ReviewController(Repository<Review> repository) : EntityController<Review>(repository)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Review entity, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        entity.UserGuid = (Guid)userGuid;
        return await base.Add(entity, ct);
    }

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Review entity, CancellationToken ct)
    {
        if (!HttpContext.IsUser(entity.UserGuid)) return Unauthorized();
        return await base.Update(entity, ct);
    }

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await repo.GetById(id.Deobfuscate().Id, ct: ct);
        if (item == null) return Problem();
        if (!HttpContext.IsUser(item.UserGuid)) return Unauthorized();

        return await base.Delete(id, ct);
    }

    [HttpGet]
    [Route("product/{id}")]
    public async Task<ActionResult<List<Review>>> GetByProduct(Guid id, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        (await repo.GetAll(restrict: x => x.ProductId == id.Deobfuscate().Id, ct: ct)).Skip(skip).Take(take).ToList();

    [HttpGet]
    [Route("product/{productId}/user/{userId}")]
    public async Task<ActionResult<List<Review>>> GetByProductAndUser(Guid id, Guid userId, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        (await repo.GetAll(restrict: x => x.ProductId == id.Deobfuscate().Id && x.UserGuid == userId, ct: ct)).Skip(skip).Take(take).ToList();

    [HttpGet]
    [Route("product/{id}/breakdown")]
    public async Task<ActionResult<int[]>> GetByProduct(Guid id, CancellationToken ct = default)
    {
        var reviews = (await repo.GetAll(restrict: x => x.ProductId == id.Deobfuscate().Id, ct: ct)).ToList();
        return reviews.GroupBy(x => x.Rate).OrderBy(g => g.Key).Select(g => g.Count()).ToArray();
    }
}