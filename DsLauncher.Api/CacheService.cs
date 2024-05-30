using DsCore.ApiClient;
using Microsoft.Extensions.Caching.Memory;

namespace DsLauncher.Api;

public class CacheService(IMemoryCache cache, DsCoreClientFactory clientFactory)
{
    readonly IMemoryCache cache = cache;
    readonly DsCoreClientFactory clientFactory = clientFactory;
    readonly TimeSpan expirationTime = TimeSpan.FromHours(1);

    public async Task<Guid> GetCurrencyGuid(string currencyName, CancellationToken ct)
    {
        if (cache.TryGetValue<Guid>(currencyName, out var result))
            return result;

        var client = clientFactory.CreateClient(string.Empty);
        var currency = await client.Currency_GetCurrency2Async(currencyName, ct);
        cache.Set(currencyName, currency.Guid, expirationTime);
        return currency.Guid;
    }
}