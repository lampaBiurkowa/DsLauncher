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
public class ProductController(
    Repository<Product> repo,
    Repository<Purchase> purchaseRepo,
    AccessContext context) : EntityController<Product>(repo)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Product entity, CancellationToken ct)
    {
        if (!await BelongsToDeveloper(entity.Guid, ct)) return Unauthorized();

        return await base.Add(entity, ct);
    }

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Product entity, CancellationToken ct)
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

    async Task<bool> BelongsToDeveloper(Guid productGuid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return false;

        var item = await repo.GetById(productGuid.Deobfuscate().Id, ct: ct);
        if (item == null) return false;
        repo.Clear();

        return await context.BelongsToDeveloper((Guid)userGuid, item.DeveloperGuid, ct);
    }
}