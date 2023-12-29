using AiKamu.Common;
using Discord.WebSocket;

namespace AiKamu.Commands;

public interface ICommand
{
    bool IsPrivateResponse(CommandArgs commandArgs);
    Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, CommandArgs commandArgs);
}

public class CommandArgs(SocketSlashCommandData slashCommandData)
{
    public bool IsPrivateResponse => (bool)(slashCommandData.Options.FirstOrDefault(o => o.Name == SlashCommandConstants.OptionNameEphemeral)?.Value ?? false);
    public Dictionary<string, object> Args { get; } = slashCommandData.Options.ToDictionary(o => o.Name, o => o.Value);
}