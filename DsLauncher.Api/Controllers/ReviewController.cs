using DibBase.Extensions;
using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsLauncher.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class ReviewController(Repository<Review> repository) : EntityController<Review>(repository)
{
    [HttpGet]
    [Route("product/{id}")]
    public async Task<ActionResult<List<Review>>> GetByProduct(Guid id, int skip = 0, int take = 1000, CancellationToken ct = default) =>
        await repo.GetAll(skip, take).Where(x => x.ProductId == id.Deobfuscate().Id).ToListAsync(ct);
}