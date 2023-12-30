using AiKamu.Common;
using Discord.WebSocket;
using Refit;
using Serilog;
using System.Text.Json;

namespace AiKamu.Commands.OpenAi;

public class OpenAi(IOpenAiApi api) : ICommand
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
            return await GetChatCompletionConversationResponse(languageModel, conversations!);
        }
        else
        {
            var message = commandArgs.Args[SlashCommandConstants.OptionNameMessage] as string;

            if (string.IsNullOrWhiteSpace(message))
            {
                return new TextResponse(false, "Sorry, something went wrong. I can't see your message");
            }

            messageType = await DetermineMessageType(message);

            return messageType switch 
            {
                _chatRequest => await GetChatCompletionResponse(languageModel, message),
                _imageRequest => await GetGeneratedImage(message),
                _ => new TextResponse(false, "I am confuse. Could you try to ask another question?")
            };
        }
    }

    private async Task<TextResponse> GetChatCompletionResponse(string languageModel, string message)
    {
        (bool IsSuccess, OpenAiResponse? Response, OpenAIError? Error) = await GetChatCompletion(languageModel, GetDefaultMessage(message));

        if (IsSuccess && Response?.Choices != null)
        {
            return new TextResponse(true, Response.Choices?.FirstOrDefault()?.Message?.Content ?? "");
        }
        else if (!IsSuccess)
        {
            return new TextResponse(false, $"Sorry, there are issues when trying to get response from OpenAI api. Error: {Error?.Error?.ErrorType}");
        }

        return new TextResponse(false, "I am confuse. Could you try to ask another question?");
    }

    private async Task<TextResponse> GetChatCompletionConversationResponse(string languageModel, List<KeyValuePair<string, string>> messages)
    {
        (bool IsSuccess, OpenAiResponse? Response, OpenAIError? Error) = await GetChatCompletion(languageModel, GetDefaultMessageFromConversation(messages));

        if (IsSuccess && Response?.Choices != null)
        {
            return new TextResponse(true, Response.Choices?.FirstOrDefault()?.Message?.Content ?? "");
        }
        else if (!IsSuccess)
        {
            return new TextResponse(false, $"Sorry, there are issues when trying to get response from OpenAI api. Error: {Error?.Error?.ErrorType}");
        }

        return new TextResponse(false, "I am confuse. Could you try to ask another question?");
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

    private static List<OpenAiMessage> GetDefaultMessage(string message)
    {
        return
        [
            new("system", "You are a Discord bot. Your name is AiKamu, usually called Ai. You are a helpful assistant."),
            new("user", message)
        ];
    }

    private static List<OpenAiMessage> GetDefaultMessageFromConversation(List<KeyValuePair<string, string>> conversations)
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

    private async Task<(bool IsSuccess, OpenAiResponse? Response, OpenAIError? error)> GetChatCompletion(string model, List<OpenAiMessage> messages)
    {
        var openAiRequest = new OpenAiRequest(model,
                messages,
                0.5,
                1000,
                0.3,
                0.5,
                0);

        Log.Information("OpenAI Request {request}", JsonSerializer.Serialize(openAiRequest));

        try
        {
            var response = await api.ChatCompletion(openAiRequest);
            Log.Information("OpenAI Response {response}", JsonSerializer.Serialize(response));

            return new(true, response, null);
        }
        catch (ApiException exception)
        {
            if (!string.IsNullOrWhiteSpace(exception?.Content))
            {
                var error = JsonSerializer.Deserialize<OpenAIError>(exception.Content);
                Log.Error(exception, exception.Content);
                return new(false, null, error);
            }
            else
            {
                Log.Error(exception, exception?.Message ?? string.Empty);
                return new(false, null, new OpenAIError { Error = new Error { Message = exception?.Message } });
            }
        }
    }

    private async Task<(bool IsSuccess, ImageGenerationResponse? Response, OpenAIError? Error)> GetImageGenerations(string prompt)
    {
        var openAIRequest = new ImageGenerationRequest("dall-e-3", prompt, 1, "1024x1024");
        Log.Information("OpenAI Request {request}", JsonSerializer.Serialize(openAIRequest));

        try
        {
            var response = await api.GenerateImages(openAIRequest);
            Log.Information("OpenAI Response {response}", JsonSerializer.Serialize(response));
            return new (true, response, null);
        }
        catch (ApiException exception)
        {
            if (!string.IsNullOrWhiteSpace(exception?.Content))
            {
                var error = JsonSerializer.Deserialize<OpenAIError>(exception.Content);
                Log.Error(exception, exception.Content);
                return new (false, null, error);
            }
            else
            {
                Log.Error(exception, exception?.Message ?? string.Empty);
                return new (false, null, new OpenAIError { Error = new Error { Message = exception?.Message} });
            }
        }
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
