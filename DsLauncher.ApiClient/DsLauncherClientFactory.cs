using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace DsLauncher.ApiClient;

public class DsLauncherClientFactory(IHttpClientFactory httpClientFactory, IOptions<DsLauncherOptions> options)
{
    readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    readonly DsLauncherOptions options = options.Value;

    public DsLauncherClient CreateClient(string bearerToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new ($"{options.Url.TrimEnd('/')}/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return new DsLauncherClient(client);
    }
}
