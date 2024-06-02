using System.Net.Http.Headers;
using HttpClientProgress;
using Microsoft.Extensions.Options;

namespace DsLauncher.ApiClient;

public class DsLauncherNdibApiClient(IOptions<DsLauncherOptions> options)
{
    readonly DsLauncherOptions options = options.Value;
    
    public async Task<Stream> ChangeToVersion(string bearerToken, Guid srcGuid, Guid dstGuid, Platform platform, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{srcGuid}/{dstGuid}/{platform}";
        return await Download(bearerToken, url, callback);
    }

    public async Task<Stream> UpdateToLatest(string bearerToken, Guid srcGuid, Platform platform, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{srcGuid}/latest/{platform}";
        return await Download(bearerToken, url, callback);
    }

    public async Task<Stream> DownloadWhole(string bearerToken, Guid productGuid, Platform platform, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{productGuid}/{platform}";
        return await Download(bearerToken, url, callback);
    }

    async Task<Stream> Download(string bearerToken, string url,  EventHandler<float> callback)
    {
        var client = GetClient(bearerToken);

        var progress = new Progress<float>();
        progress.ProgressChanged += callback;

        using var stream = new MemoryStream();
        await client.DownloadDataAsync(url, stream, progress);
        return stream;
    }

    HttpClient GetClient(string bearerToken)
    {
        var client = new HttpClient()
        {
            BaseAddress = new Uri($"{options.Url.Trim('/')}")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return client;
    }
}