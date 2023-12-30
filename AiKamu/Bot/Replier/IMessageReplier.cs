using AiKamu.Commands;
using Discord.WebSocket;

namespace AiKamu.Bot.Replier;

public interface IMessageReplier
{
    Task Reply(SocketMessage message, IResponse response);
}
