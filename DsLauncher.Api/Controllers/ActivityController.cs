using DibBase.Infrastructure;
using DsCore.ApiClient;
using DsLauncher.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Authorize]
[Route("[controller]")]
public class ActivityController(Repository<Activity> activityRepo, Repository<Game> gameRepo) : ControllerBase
{
    readonly TimeSpan ActivityLastingTreshold = TimeSpan.FromSeconds(25);

    [Authorize]
    [HttpPost("report")]
    public async Task<ActionResult> ReportActivity(Activity newActivity, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null || userGuid != newActivity.UserGuid) return Unauthorized();

        var oldActivity = (await activityRepo.GetAll(restrict: x => x.StartDate == newActivity.StartDate && x.UserGuid == newActivity.UserGuid && x.ProductGuid == newActivity.ProductGuid, ct: ct)).FirstOrDefault();
        if (oldActivity == null)
            await activityRepo.InsertAsync(newActivity, ct);
        else
            await activityRepo.UpdateAsync(newActivity, ct);
        
        await activityRepo.CommitAsync(ct);
        return Ok();
    }

    [Authorize]
    [HttpPost("current-game/{userGuid}")]
    public async Task<ActionResult<Guid?>> GetCurrentGame(Guid userGuid, CancellationToken ct)
    {
        var results = await activityRepo.GetAll(restrict: x => x.UserGuid == userGuid && x.EndDate + ActivityLastingTreshold > DateTime.UtcNow, ct: ct);
        var games = await gameRepo.GetByIds(results.Select(x => x.ProductId), ct: ct);

        return Ok(games.FirstOrDefault()?.Guid);
    }
}