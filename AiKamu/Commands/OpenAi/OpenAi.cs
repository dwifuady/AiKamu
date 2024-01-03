using AiKamu.Common;
using Discord.WebSocket;
using Serilog;
using System.Text.Json;

namespace AiKamu.Commands.OpenAi;

public sealed class OpenAi(IOpenAiApi api) : BaseOpenAi(api), ICommand
{
    private const string _chatRequest = "chat";
    private const string _imageRequest = "draw";
    public bool IsPrivateResponse(CommandArgs commandArgs)
    {
        return commandArgs.IsPrivateResponse;
    }

    public async Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, CommandArgs commandArgs)
    {
        Log.Information("[{Command}] GetResponseAsync, Args {args}", nameof(OpenAi), JsonSerializer.Serialize(commandArgs.Args));

        _ = commandArgs.Args.TryGetValue(SlashCommandConstants.OptionNameConversation, out object? messageChainObj);
        _ = commandArgs.Args.TryGetValue(SlashCommandConstants.OptionNameLanguageModel, out object? model);
        var languageModel = model as string ?? SlashCommandConstants.OptionChoice35Turbo;

        string messageType = _chatRequest;

        // conversation
        if (messageChainObj != null)
        {
            var conversations = messageChainObj as List<KeyValuePair<string, string>>;
            return await GetChatCompletionResponse(languageModel, GetDefaultMessage(conversations!));
        }
        else
        {
            var message = commandArgs.Args[SlashCommandConstants.OptionNameMessage] as string;
            
            if (string.IsNullOrWhiteSpace(message))
            {
                return new TextResponse(false, "Sorry, something went wrong. I can't see your message");
            }

            messageType = await DetermineMessageType(message);
            List<KeyValuePair<string, string>> messages = [new(RoleConstants.RoleUser, message)];
            
            return messageType switch 
            {
                _chatRequest => await GetChatCompletionResponse(languageModel, GetDefaultMessage(messages)),
                _imageRequest => await GetGeneratedImage(message),
                _ => new TextResponse(false, "I am confuse. Could you try to ask another question?")
            };
        }
    }

    private async Task<IResponse> GetGeneratedImage(string prompt)
    {
        var (IsSuccess, Response, OpenAiErrorResponse) = await GetImageGenerations(prompt);
        if (IsSuccess && Response?.Data is not null && Response.Data.Count != 0)
        {
            return new ImageResponse(true, Response.Data.FirstOrDefault()!.Url!, prompt);
        }
        else if (!IsSuccess)
        {
            return new TextResponse(false, $"Sorry, there are issues when trying to get response from OpenAI api. Error: {OpenAiErrorResponse?.Error?.ErrorType}");
        }

        return new TextResponse(false, "I am confuse. Could you try to ask another question?");
    }

    private static List<OpenAiMessage> GetDefaultMessage(List<KeyValuePair<string, string>> conversations)
    {
        var messages = new List<OpenAiMessage>
        {
            new("system", "You are a Discord bot. Your name is AiKamu, usually called Ai. You are a helpful assistant.")
        };

        foreach (var conversation in conversations)
        {
            messages.Add(new(conversation.Key, conversation.Value));
        }

        return messages;
    }

    

    private async Task<string> DetermineMessageType(string message)
    {
        List<OpenAiMessage> prompt =
        [
            new("user", $"Determine the given message. Wether it's a normal chat or requesting to draw an image. just response with 'chat' or 'draw'. If it's unclear, reply with 'none'. \"{message}\"")
        ];
        
        var (IsSuccess, Response, _) = await GetChatCompletion(SlashCommandConstants.OptionChoice35Turbo, prompt);

        if (!IsSuccess)
        {
            return _chatRequest;
        }

        return Response?.Choices?.FirstOrDefault()?.Message?.Content ?? _chatRequest;
    }
}
