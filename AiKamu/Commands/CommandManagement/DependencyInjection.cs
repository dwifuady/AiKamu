using AiKamu.Common;

namespace AiKamu.Commands.AddCommand;

public static class DependencyInjection
{
    public static IServiceCollection AddCommandManagement(this IServiceCollection services)
    {
        services.AddKeyedTransient<ICommand, CommandManagement.CommandManagement>(SlashCommandConstants.CommandNameManageCommand);
        
        return services;
    }
}
