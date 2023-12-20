namespace AiKamu.Commands.OpenAi;

using System.Text.Json.Serialization;
public class Choice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public object? FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class OpenAiResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class OpenAIError
{
    [JsonPropertyName("error")]
    public Error? Error { get; set; }
}

public class Error
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("type")]
    public string? ErrorType { get; set; }
    [JsonPropertyName("param")]
    public string? ErrorParam { get; set; }
    [JsonPropertyName("code")]
    public string? ErrorCode { get; set; }
}