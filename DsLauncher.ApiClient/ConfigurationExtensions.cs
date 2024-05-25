using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DsLauncher.ApiClient;

public static class ConfigurationExtensions
{
    public static void AddDsLauncher(this IConfiguration configuration, IServiceCollection services)
    {
        services.AddOptions<DsLauncherOptions>()
            .Bind(configuration.GetSection(DsLauncherOptions.SECTION));

        services.AddHttpClient();
        services.AddTransient<DsLauncherClientFactory>();
    }
}
