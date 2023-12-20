using AiKamu.Common;
using Discord.WebSocket;
using Refit;
using Serilog;
using System.Text.Json;

namespace AiKamu.Commands.OpenAi;

public class OpenAi(IOpenAiApi api) : ICommand
{
    public bool IsPrivateResponse(SocketSlashCommandData data)
    {
        return (bool)(data.Options.FirstOrDefault(o => o.Name == SlashCommandConstants.OptionNameEphemeral)?.Value ?? false);
    }

    public async Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, SocketSlashCommandData data)
    {
        var prompt = data?.Options?.FirstOrDefault(x => x.Name == SlashCommandConstants.OptionNamePrompt)?.Value as string;
        var languageModel = data?.Options?.FirstOrDefault(x => x.Name == SlashCommandConstants.OptionNameLanguageModel)?.Value as string ?? SlashCommandConstants.OptionChoice35Turbo;
        
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new TextResponse(false, "Sorry, something went wrong. I can't see your message");
        }

        (bool IsSuccess, OpenAiResponse? Response, OpenAIError? Error) = await GetChatCompletion(languageModel, prompt);

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

    private async Task<(bool IsSuccess, OpenAiResponse? Response, OpenAIError? error)> GetChatCompletion(string model, string prompt)
    {
        var openAiRequest = new OpenAiRequest(model,
                GetDefaultMessage(prompt),
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

    private static List<OpenAiMessage> GetDefaultMessage(string prompt)
    {
        return
        [
            new("system", "You are a helpful assistant."),
            new("user", prompt)
        ];
    }
}
