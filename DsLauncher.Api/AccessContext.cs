using DibBase.Extensions;
using DibBase.Infrastructure;
using DsLauncher.Api.Models;

namespace DsLauncher.Api;

public class AccessContext(IServiceProvider serviceProvider)
{
    public async Task<bool> BelongsToDeveloper(Guid userGuid, Guid developerGuid, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Developer>>();
        var developer = await repo.GetById(developerGuid.Deobfuscate().Id, ct: ct);
        return developer != null && developer.UserGuids.Contains(userGuid);
    }
}