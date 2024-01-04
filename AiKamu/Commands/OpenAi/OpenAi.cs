using AiKamu.Common;
using Discord.WebSocket;
using Serilog;
using System.Text.Json;

namespace AiKamu.Commands.OpenAi;

public sealed class OpenAi(IOpenAiApi api) : BaseOpenAi(api), ICommand
{
    private const string _chatRequest = "Question";
    private const string _imageRequest = "ImageGeneration";
    private const string _visionRequest = "Vision";
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

            // Check quoted message
            _ = commandArgs.Args.TryGetValue(SlashCommandConstants.OptionNameQuotedMessageText, out var quotedMessageObj);
            var quotedMessage = quotedMessageObj as string;

            // Combine message and quoted message
            var fullMessage = $"{message} \"{quotedMessage}\"";
            
            if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(quotedMessage))
            {
                return new TextResponse(false, "Sorry, something went wrong. I can't see your message");
            }

            messageType = await DetermineMessageType(fullMessage);
            List<KeyValuePair<string, string>> messages = [new(RoleConstants.RoleUser, fullMessage)];
            
            return messageType switch 
            {
                _chatRequest => await GetChatCompletionResponse(languageModel, GetDefaultMessage(messages)),
                _imageRequest => await GetGeneratedImage(fullMessage),
                _visionRequest => await GetVisionResult(fullMessage, commandArgs.Args),
                _ => await GetChatCompletionResponse(languageModel, GetDefaultMessage(messages))
                // _ => new TextResponse(false, "I am confuse. Could you try to ask another question?")
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

    private async Task<TextResponse> GetVisionResult(string message, IReadOnlyDictionary<string, object> args)
    {
        _ = args.TryGetValue(SlashCommandConstants.OptionNameImageUrl, out var imageUrlObj);
        var imageUrl = imageUrlObj as string;
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return new TextResponse(false, "Sorry, I can't see your image.");
        }

        var (IsSuccess, Response, OpenAiErrorResponse) = await GetVisionCompletions(message, imageUrl);

        if (IsSuccess && Response?.Choices != null)
        {
            return new TextResponse(true, Response.Choices?.FirstOrDefault()?.Message?.Content ?? "");
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
            new("user", $"These are available message type of a text prompt: Question, ImageGeneration, Vision. Which one is this query asking for? If none match, respond with Unknown. query: '{message}' MessageType:")
        ];
        
        var (IsSuccess, Response, _) = await GetChatCompletion(SlashCommandConstants.OptionChoice35Turbo, prompt);

        if (!IsSuccess)
        {
            return _chatRequest;
        }

        return Response?.Choices?.FirstOrDefault()?.Message?.Content ?? _chatRequest;
    }
}
