using Discord.WebSocket;

namespace AiKamu.Commands;

public interface ICommand
{
    bool IsPrivateResponse(SocketSlashCommandData data);
    Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, SocketSlashCommandData data);
}