using AiKamu.Common;
using Discord.WebSocket;

namespace AiKamu.Commands.OpenAi;

public sealed class TranslateEn(IOpenAiApi api) : BaseOpenAi(api), ICommand
{
    public async Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, CommandArgs commandArgs)
    {
        var message = commandArgs.Args[SlashCommandConstants.OptionNameMessage] as string;
        if (string.IsNullOrWhiteSpace(message))
        {
            return new TextResponse(false, "Sorry, something went wrong. I can't see your message");
        }

        return await GetChatCompletionResponse(SlashCommandConstants.OptionChoice35Turbo, GetDefaultMessage(message));
    }

    private static List<OpenAiMessage> GetDefaultMessage(string message)
    {
        return
        [
            new("system", "You are a translator who translate given message to English"),
            new("user", message)
        ];
    }
    
    public bool IsPrivateResponse(CommandArgs commandArgs)
    {
        return true;
    }
}
