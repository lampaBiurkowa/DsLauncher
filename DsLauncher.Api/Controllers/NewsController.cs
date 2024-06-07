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
public class NewsController(Repository<News> repository, AccessContext context) : EntityController<News>(repository)
{
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(News entity, CancellationToken ct)
    {
        if (!await OperationAllowed(entity.Guid, ct)) return Unauthorized();
        return await base.Add(entity, ct);
    }

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(News entity, CancellationToken ct)
    {
        if (!await OperationAllowed(entity.Guid, ct)) return Unauthorized();
        return await base.Update(entity, ct);
    }

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid guid, CancellationToken ct)
    {
        if (!await OperationAllowed(guid, ct)) return Unauthorized();
        return await base.Delete(guid, ct);
    }

    [HttpGet("product/{guid}")]
    public async Task<ActionResult<List<News>>> GetByProduct(Guid guid, CancellationToken ct)
    {
        var news = await repo.GetAll(restrict: x => x.ProductId == guid.Deobfuscate().Id, ct: ct);
        return Ok(news.Select(x => IdHelper.HidePrivateId(x)));
    }
    
    [HttpGet("developer/{guid}")]
    public async Task<ActionResult<List<News>>> GetByDeveloper(Guid guid, CancellationToken ct)
    {
        var news = await repo.GetAll(restrict: x => x.Product != null && x.Product.DeveloperId == guid.Deobfuscate().Id, expand: [x => x.Product], ct: ct);
        return Ok(news.Select(x => IdHelper.HidePrivateId(x)));
    }

    async Task<bool> OperationAllowed(Guid newsGuid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return false;

        var item = await repo.GetById(newsGuid.Deobfuscate().Id, [x => x.Product!], ct);
        if (item == null) return false;
        repo.Clear();

        return item.Product == null || await context.BelongsToDeveloper((Guid)userGuid, item.Product.DeveloperGuid, ct);
    }
}