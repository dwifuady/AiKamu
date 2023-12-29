using AiKamu.Commands;
using Discord.WebSocket;

namespace AiKamu.Bot.Replier;

public interface ISlashCommandReplier
{
    Task Reply(SocketSlashCommand slashCommand, bool privateReply, IResponse response);
}
