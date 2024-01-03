using AiKamu.Common;
using Refit;

namespace AiKamu.Commands.OpenAi;

public static class OpenAiServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAi(this IServiceCollection services)
    {
        services.AddKeyedTransient<ICommand, OpenAi>(SlashCommandConstants.CommandNameAI);
        services.AddKeyedTransient<ICommand, TranslateId>(SlashCommandConstants.CommandNameTranslateId);
        services.AddKeyedTransient<ICommand, TranslateEn>(SlashCommandConstants.CommandNameTranslateEn);
        services.AddTransient<AuthHeaderHandler>();
        services.AddRefitClient<IOpenAiApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openai.com"))
            .AddHttpMessageHandler<AuthHeaderHandler>();
        return services;
    }
}
