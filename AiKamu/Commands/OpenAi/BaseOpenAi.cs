using System.Text.Json;
using AiKamu.Common;
using Refit;
using Serilog;

namespace AiKamu.Commands.OpenAi;

public class BaseOpenAi(IOpenAiApi api)
{
    protected async Task<TextResponse> GetChatCompletionResponse(string languageModel, List<OpenAiMessage> messages)
    {
        (bool IsSuccess, OpenAiResponse? Response, OpenAIError? Error) = await GetChatCompletion(languageModel, messages);

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

    protected async Task<(bool IsSuccess, OpenAiResponse? Response, OpenAIError? error)> GetChatCompletion(string model, List<OpenAiMessage> messages)
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

    protected async Task<(bool IsSuccess, ImageGenerationResponse? Response, OpenAIError? Error)> GetImageGenerations(string prompt)
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

    protected async Task<(bool IsSuccess, OpenAiResponse? Reponse, OpenAIError? Error)> GetVisionCompletions(string message, string imageUrl)
    {
        var contents = new List<Content>
        {
            new("text", message),
            new("image_url", new ImageUrl(imageUrl))
        };

        var messages = new List<Message>
        {
            new(RoleConstants.RoleUser, contents)
        };
        
        var openAiRequest = new OpenAiVisionRequest("gpt-4-vision-preview", messages, 1000);

        Log.Information("OpenAI Request {request}", JsonSerializer.Serialize(openAiRequest));
        try
        {
            var response = await api.VisionCompletion(openAiRequest);
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
}
