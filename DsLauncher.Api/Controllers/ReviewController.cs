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
        (await repo.GetAll(restrict: x => x.ProductDsId.Guid == id, ct: ct)).Skip(skip).Take(take).ToList();

    [HttpGet]
    [Route("product/{id}/breakdown")]
    public async Task<ActionResult<int[]>> GetByProduct(Guid id, CancellationToken ct = default)
    {
        var reviews = (await repo.GetAll(restrict: x => x.ProductDsId.Guid == id, ct: ct)).ToList();
        return reviews.GroupBy(x => x.Rate).OrderBy(g => g.Key).Select(g => g.Count()).ToArray();
    }
}