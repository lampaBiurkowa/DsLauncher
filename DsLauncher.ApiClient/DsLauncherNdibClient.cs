using System.Net.Http.Headers;
using HttpClientProgress;
using Microsoft.Extensions.Options;

namespace DsLauncher.ApiClient;

public class DsLauncherNdibApiClient(IOptions<DsLauncherOptions> options)
{
    readonly DsLauncherOptions options = options.Value;
    
    public async Task ChangeToVersion(string bearerToken, Guid srcGuid, Guid dstGuid, Platform platform, Stream stream, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{srcGuid}/{dstGuid}/{platform.ToString().ToLower()}";
        await Download(bearerToken, url, stream, callback);
    }

    public async Task<Guid> UpdateToLatest(string bearerToken, Guid srcGuid, Platform platform, Stream stream, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{srcGuid}/latest/{platform.ToString().ToLower()}";
        return await Download(bearerToken, url, stream, callback);
    }

    public async Task<Guid> DownloadWhole(string bearerToken, Guid productGuid, Platform platform, Stream stream, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{productGuid}/{platform.ToString().ToLower()}";
        return await Download(bearerToken, url, stream, callback);
    }

    public async Task<Guid> DownloadWholeVersion(string bearerToken, Guid productGuid, Platform platform, Guid packageGuid, Stream stream, EventHandler<float> callback)
    {
        var url = $"Ndib/download/{productGuid}/{platform.ToString().ToLower()}/{packageGuid}";
        return await Download(bearerToken, url, stream, callback);
    }

    async Task<Guid> Download(string bearerToken, string url, Stream stream, EventHandler<float> callback)
    {
        var client = GetClient(bearerToken);

        var progress = new Progress<float>();
        progress.ProgressChanged += callback;

        var latestPackageGuid = await client.DownloadDataAsync(url, stream, progress);
        return latestPackageGuid;
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