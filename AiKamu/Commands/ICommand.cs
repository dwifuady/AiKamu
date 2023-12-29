using Discord.WebSocket;

namespace AiKamu.Commands;

public interface ICommand
{
    bool IsPrivateResponse(CommandArgs commandArgs);
    Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, CommandArgs commandArgs);
}