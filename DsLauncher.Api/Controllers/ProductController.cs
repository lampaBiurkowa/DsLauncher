using System.Linq.Expressions;
using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBase.ModelBase;
using DibBaseSampleApi.Controllers;
using DibDataGenerator;
using DsLauncher.Infrastructure;
using DsLauncher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class ProductController(Repository<Product> repository, DsLauncherContext ctx) : EntityController<Product>(repository)
{
    [HttpGet]
    [Route("a")]
    public void Ping()
    {
        EntityPopulator.Build(ctx);
    }

    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<Guid>> Add(Product entity, CancellationToken ct) => await base.Add(entity, ct);

    [Authorize]
    [HttpPut]
    public override async Task<ActionResult<Guid>> Update(Product entity, CancellationToken ct) => await base.Update(entity, ct);

    [Authorize]
    [HttpDelete]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken ct) => await base.Delete(id, ct);

    [HttpGet]
    [Route("developer/{id}")]
    public async Task<ActionResult<List<Product>>> GetByDeveloper(Guid id, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        await repo.GetAll(skip, take).Where(x => x.DeveloperId == id.Deobfuscate().Id).ToListAsync(ct);
}