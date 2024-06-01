// using DibBase.Infrastructure;
// using DibBase.Models;
// using DsLauncher.Events;
// using DsNotifier.Client;
// using Newtonsoft.Json;

// namespace DsLauncher.Api.Services;

// class EventService(IServiceProvider sp) : BackgroundService
// {
//     readonly TimeSpan checkInterval = TimeSpan.FromSeconds(5);

//     protected override async Task ExecuteAsync(CancellationToken ct)
//     {
//         PurchasedEvent a; //█▬█ █ ▀█▀
//         while (!ct.IsCancellationRequested)
//         {
//             await PollEvents(ct);
//             await Task.Delay(checkInterval, ct);
//         }
//     }

//     async Task PollEvents(CancellationToken ct)
//     {
//         using var scope = sp.CreateScope();
//         var eventRepo = scope.ServiceProvider.GetRequiredService<Repository<Event>>();
//         var client = scope.ServiceProvider.GetRequiredService<IDsNotifierClient>();
//         var events = await eventRepo.GetAll(restrict: x => !x.IsPublished, ct: ct);
//         foreach (var e in events)
//             await HandleEvent(e, client, eventRepo, ct);

//         if (events.Count > 0) await eventRepo.CommitAsync(ct);
//     }

//     static async Task HandleEvent(Event e, IDsNotifierClient client, Repository<Event> eventRepo, CancellationToken ct)
//     {
//         var type = GetTypeFromFullName(e.Name);
//         if (type != null)
//         {
//             var obj = JsonConvert.DeserializeObject(e.Payload, type);
//             if (obj != null)
//             {
//                 await client.SendGeneric(obj, type, ct);
//                 e.IsPublished = true;
//             }
//         }
//         else
//             e.IsPublished = true;
        
//         await eventRepo.UpdateAsync(e, ct: ct);
//     }

//     static Type? GetTypeFromFullName(string fullName)
//     {
//         return Type.GetType(fullName) ?? AppDomain.CurrentDomain.GetAssemblies()
//             .SelectMany(a => a.GetTypes())
//             .FirstOrDefault(t => t.FullName == fullName);
//     }
// }
