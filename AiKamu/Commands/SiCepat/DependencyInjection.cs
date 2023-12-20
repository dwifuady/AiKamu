using AiKamu.Common;
using Microsoft.Extensions.Options;
using Refit;
using Serilog;

namespace AiKamu.Commands.SiCepat;

public static class DependencyInjection
{
    public static IServiceCollection AddSiCepat(this IServiceCollection services, SiCepatConfig? config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.ApiBaseAddress))
        {
            Log.Error("SiCepat config is empty");
            return services;
        }

        services.AddKeyedTransient<ICommand, SiCepat>(SlashCommandConstants.CommandNameSicepat);
        services.AddRefitClient<ISiCepatApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(config.ApiBaseAddress));
        return services;
    }
}
