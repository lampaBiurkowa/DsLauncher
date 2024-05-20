using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsLauncher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsLauncher.Api;

[ApiController]
[Authorize]
[Route("[controller]")]
public class ProductController(Repository<Product> repository) : EntityController<Product>(repository)
{
    [HttpGet]
    [Route("developer/{id}")]
    public async Task<ActionResult<List<Product>>> GetByDeveloper(Guid id, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        await repo.GetAll(skip, take).Where(x => x.DeveloperId == id.Deobfuscate().Id).ToListAsync(ct);
}