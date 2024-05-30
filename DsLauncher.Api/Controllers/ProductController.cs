using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseApi;
using DibBaseSampleApi.Controllers;
using DsCore.ApiClient;
using DsLauncher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class ProductController(Repository<Product> repo, Repository<Purchase> purchaseRepo) : EntityController<Product>(repo)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Product entity, CancellationToken ct) => await base.Add(entity, ct);

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Product entity, CancellationToken ct)
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

    [HttpGet("search")]
    public async Task<ActionResult<List<Product>>> Search(string query, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        (await repo.GetAll(restrict: x => x.Name.Contains(query), ct: ct)).Skip(skip).Take(take).Select(IdHelper.HidePrivateId).ToList();

    [HttpGet("developer/{id}")]
    public async Task<ActionResult<List<Product>>> GetByDeveloper(Guid id, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        (await repo.GetAll(restrict: x => x.DeveloperId == id.Deobfuscate().Id, ct: ct)).Skip(skip).Take(take).Select(IdHelper.HidePrivateId).ToList();

    [HttpGet("get-id/{name}")]
    public async Task<ActionResult<Guid?>> GetId(string name, CancellationToken ct = default) =>
        (await repo.GetAll(restrict: x => x.Name == name, ct: ct)).FirstOrDefault()?.Guid;

    [Authorize]
    [HttpGet("user")]
    public async Task<ActionResult<List<Guid>>> GetByUser(CancellationToken ct = default)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var purchases = await purchaseRepo.GetAll(restrict: x => x.UserGuid == userGuid, ct: ct);
        return Ok(purchases.Select(x => x.ProductGuid));
    }
}