using DibBase.Infrastructure;
using DibBase.Models;
using Newtonsoft.Json;

namespace DsLauncher.Api.Services;

class SubscriptionService(IServiceProvider sp) : BackgroundService
{
    readonly TimeSpan checkInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = sp.CreateScope();
            var eventRepo = scope.ServiceProvider.GetRequiredService<Repository<Event>>();
            var events = await eventRepo.GetAll(restrict: x => !x.IsPublished, ct: ct);
            foreach (var e in events)
            {
                var type = GetTypeFromFullName(e.Name);
                if (type != null)
                {
                    var obj = JsonConvert.DeserializeObject(e.Payload, type);
                }
            }
            
            await Task.Delay(checkInterval, ct);
        }
    }

    static Type? GetTypeFromFullName(string fullName)
    {
        return Type.GetType(fullName) ?? AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(a => a.GetTypes())
                   .FirstOrDefault(t => t.FullName == fullName);
    }
}
