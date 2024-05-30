using DibBase.Extensions;
using DibBase.Infrastructure;
using DsCore.ApiClient;
using DsCryptoLib;
using DsLauncher.Events;
using DsLauncher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class PurchaseController(
    DsCoreClientFactory dsCoreClientFactory,
    Repository<Purchase> purchaseRepo,
    Repository<Product> productRepo,
    Repository<Subscription> subscriptionRepo,
    Repository<License> licenseRepo,
    Repository<Developer> developerRepo,
    CacheService cache) : ControllerBase
{
    readonly DsCoreClientFactory dsCoreClientFactory = dsCoreClientFactory;
    readonly Repository<Purchase> purchaseRepo = purchaseRepo;
    readonly Repository<Product> productRepo = productRepo;
    readonly Repository<Subscription> subscriptionRepo = subscriptionRepo;
    readonly CacheService cache = cache;
    const string DEFAULT_CURRENCY = "Ruble";
    const float DEVELOPER_ACCESS_PRICE = 5;
    readonly TimeSpan DeveloperAccessPaymentInterval = TimeSpan.FromDays(30);

    [Authorize]
    [HttpPost("product/{guid}")]
    public async Task<ActionResult<bool>> PurchaseProduct(Guid guid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var product = await productRepo.GetById(guid.Deobfuscate().Id, ct: ct);
        if (product == null) return Problem();

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_PayOnceAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = -product.Price,
            CurrencyGuid = await cache.GetCurrencyGuid(DEFAULT_CURRENCY, ct)
        }, ct);

        if (result == null) return Ok(false);

        await purchaseRepo.InsertAsync(new()
        {
            ProductGuid = guid,
            TransactionGuid = (Guid)result,
            UserGuid = (Guid)userGuid
        }, ct);
        await purchaseRepo.RegisterEvent(new PurchasedEvent { ProductGuid = guid, UserGuid = (Guid)userGuid }, ct);
        await purchaseRepo.CommitAsync(ct);

        return Ok(true);
    }

    [Authorize]
    [HttpPost("developer-access/{guid}")]
    public async Task<ActionResult<bool>> PurchaseDeveloperAccess(Guid guid, string developerKey, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var developer = await developerRepo.GetById(guid.Deobfuscate().Id, ct: ct);
        if (developer == null) return Problem();

        var license = (await licenseRepo.GetAll(restrict: x => x.DeveloperId == guid.Deobfuscate().Id, ct: ct)).FirstOrDefault();
        if (license == null) return Unauthorized();

        var passwordNewHash = SecretsBuilder.CreatePasswordHash(developerKey, license.Salt);
        if (license.Key != passwordNewHash) return Unauthorized();

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_AddCyclicFeeAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = -DEVELOPER_ACCESS_PRICE,
            CurrencyGuid = await cache.GetCurrencyGuid(DEFAULT_CURRENCY, ct)
        }, DeveloperAccessPaymentInterval, ct);

        if (result == null) return Ok(false);
        
        developer.UserGuids.Add((Guid)userGuid);
        await developerRepo.UpdateAsync(developer, ct);
        await developerRepo.RegisterEvent(new BecameDeveloperEvent { DeveloperGuid = guid, UserGuid = (Guid)userGuid }, ct);
        await developerRepo.CommitAsync(ct);

        return Ok(true);
    }

    [Authorize]
    [HttpPost("developer-access")]
    public async Task<ActionResult<string>> RegisterDeveloperAccess(Developer developer, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        developer.UserGuids = [];
        var password = SecretsBuilder.GenerateSalt(32);
        var salt = SecretsBuilder.GenerateSalt(16);
        var passwordHash = SecretsBuilder.CreatePasswordHash(password, salt);
        await licenseRepo.InsertAsync(new()
        {
            Key = passwordHash,
            Salt = salt,
            Developer = developer
        }, ct);
        await licenseRepo.CommitAsync(ct);

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_AddCyclicFeeAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = -DEVELOPER_ACCESS_PRICE,
            CurrencyGuid = await cache.GetCurrencyGuid(DEFAULT_CURRENCY, ct)
        }, DeveloperAccessPaymentInterval, ct);

        if (result == null) return Problem();

        developer.UserGuids.Add((Guid)userGuid);
        await developerRepo.UpdateAsync(developer, ct);
        await developerRepo.CommitAsync(ct);

        //TODO wrap in a parent transaction
        await developerRepo.RegisterEvent(new BecameDeveloperEvent { DeveloperGuid = developer.Guid, UserGuid = (Guid)userGuid }, ct);
        await developerRepo.CommitAsync(ct);

        return Ok(password); // :D/
    }

    [Authorize]
    [HttpPost("subscribe-developer/{guid}")]
    public async Task<ActionResult> SubscribeDeveloper(Guid guid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var developer = await developerRepo.GetById(guid.Deobfuscate().Id, ct: ct);
        if (developer == null) return Problem();

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_AddCyclicFeeAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = -developer.SubscriptionPrice,
            CurrencyGuid = await cache.GetCurrencyGuid(DEFAULT_CURRENCY, ct)
        }, DeveloperAccessPaymentInterval, ct);

        if (result == null) return Problem();

        await subscriptionRepo.InsertAsync(new()
        {
            DeveloperGuid = guid,
            CyclicFeeGuid = (Guid)result,
            UserGuid = (Guid)userGuid
        }, ct);
        await subscriptionRepo.RegisterEvent(new SubscribedEvent { DeveloperGuid = guid, UserGuid = (Guid)userGuid }, ct);
        await subscriptionRepo.CommitAsync(ct);

        return Ok();
    }

    [Authorize]
    [HttpDelete("subscribe-developer/{guid}/cancel")]
    public async Task<ActionResult> CancelDeveloperSubscription(Guid guid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var developer = await developerRepo.GetById(guid.Deobfuscate().Id, ct: ct);
        if (developer == null) return Problem();

        var subscription = (await subscriptionRepo.GetAll(restrict: x => x.UserGuid == userGuid && x.DeveloperGuid == guid, ct: ct)).FirstOrDefault();
        if (subscription == null) return Problem();

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_CancelCyclicFeeAsync(subscription.CyclicFeeGuid, ct);

        await subscriptionRepo.DeleteAsync(subscription.Id, ct);
        await subscriptionRepo.RegisterEvent(new UnsubscribedEvent { DeveloperGuid = guid, UserGuid = (Guid)userGuid }, ct);
        await subscriptionRepo.CommitAsync(ct);

        return Ok();
    }
}