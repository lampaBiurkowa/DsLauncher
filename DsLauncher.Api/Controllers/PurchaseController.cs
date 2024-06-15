using DibBase.Extensions;
using DibBase.Infrastructure;
using DsCore.ApiClient;
using DsCryptoLib;
using DsLauncher.Events;
using DsLauncher.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DsLauncher.Api.Options;
using Microsoft.Extensions.Options;

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
    IOptions<LauncherOptions> options,
    CacheService cache) : ControllerBase
{
    const string DEFAULT_CURRENCY = "Ruble";
    readonly TimeSpan licenseValidity = TimeSpan.FromMinutes(5);

    [Authorize]
    [HttpPost("product/{guid}")]
    public async Task<ActionResult<bool>> PurchaseProduct(Guid guid, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var product = await productRepo.GetById(guid.Deobfuscate().Id, ct: ct);
        if (product == null) return Problem();

        var userSubscribed = (await subscriptionRepo.GetAll(restrict: x => x.UserGuid == userGuid && x.DeveloperGuid == guid, ct: ct)).Count != 0;
        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_PayOnceAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = userSubscribed ? 0 : -product.Price,
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
    [HttpPost("developer-access")]
    public async Task<ActionResult<bool>> PurchaseDeveloperAccess(string developerKey, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var passwordNewHash = SecretsBuilder.CreatePasswordHash(developerKey, string.Empty);
        var license = (await licenseRepo.GetAll(restrict: x => x.Key == passwordNewHash && x.ValidTo < DateTime.UtcNow, ct: ct)).FirstOrDefault();
        if (license == null) return Unauthorized();

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_AddCyclicFeeAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = -options.Value.DeveloperAccessPrice,
            CurrencyGuid = await cache.GetCurrencyGuid(DEFAULT_CURRENCY, ct)
        }, options.Value.CyclicPaymentInterval, ct);

        if (result == null) return Ok(false);
        
        
        var developer = await developerRepo.GetById(license.DeveloperId, ct: ct);
        if (developer == null) return Problem();

        developer.UserGuids.Add((Guid)userGuid);
        await developerRepo.UpdateAsync(developer, ct);
        await developerRepo.RegisterEvent(new BecameDeveloperEvent { DeveloperGuid = developer.Guid, UserGuid = (Guid)userGuid }, ct);
        await developerRepo.CommitAsync(ct);

        return Ok(true);
    }

    [Authorize]
    [HttpPost("developer-access/new")]
    public async Task<ActionResult<string>> RegisterDeveloperAccess(Developer developer, CancellationToken ct)
    {
        var userGuid = HttpContext.GetUserGuid();
        if (userGuid == null) return Unauthorized();

        var client = dsCoreClientFactory.CreateClient(HttpContext.GetBearerToken()!);
        var result = await client.Billing_AddCyclicFeeAsync(new()
        {
            UserGuid = (Guid)userGuid,
            Value = -options.Value.DeveloperAccessPrice,
            CurrencyGuid = await cache.GetCurrencyGuid(DEFAULT_CURRENCY, ct)
        }, options.Value.CyclicPaymentInterval, ct);

        if (result == null) return Problem();

        developer.UserGuids = [((Guid)userGuid)];
        await developerRepo.UpdateAsync(developer, ct);
        await developerRepo.RegisterEvent(new BecameDeveloperEvent { DeveloperGuid = developer.Guid, UserGuid = (Guid)userGuid }, ct);
        await developerRepo.CommitAsync(ct);

        return Ok();
    }

    [Authorize]
    [HttpGet("developer-access/{guid}/generate-key")]
    public async Task<ActionResult<string>> GenerateKey(Guid guid, CancellationToken ct)
    {
        var password = SecretsBuilder.GenerateSalt(32);
        var passwordHash = SecretsBuilder.CreatePasswordHash(password, string.Empty);
        await licenseRepo.InsertAsync(new()
        {
            Key = passwordHash,
            DeveloperId = guid.Deobfuscate().Id,
            ValidTo = DateTime.UtcNow + licenseValidity
        }, ct);
        await licenseRepo.CommitAsync(ct);

        return Ok(password); // :D/
    }

    [HttpGet("developer-access/price")]
    public ActionResult<float> GetDeveloperAccessPrice() => Ok(options.Value.DeveloperAccessPrice);

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
        }, options.Value.CyclicPaymentInterval, ct);

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