using System.Text.Json.Serialization;

namespace AiKamu.Commands.OpenAi;

public class OpenAiMessage(string role, string content)
{
    [JsonPropertyName("role")]
    public string Role { get; } = role;

    [JsonPropertyName("content")]
    public string? Content { get; } = content;
}

public class OpenAiRequest(string model, IReadOnlyList<OpenAiMessage> messages, double temperature, int maxTokens, double topP, double frequencyPenalty, int presencePenalty)
{
    [JsonPropertyName("model")]
    public string Model { get; } = model;

    [JsonPropertyName("messages")]
    public IReadOnlyList<OpenAiMessage> Messages { get; } = messages;

    [JsonPropertyName("temperature")]
    public double Temperature { get; } = temperature;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; } = maxTokens;

    [JsonPropertyName("top_p")]
    public double TopP { get; } = topP;

    [JsonPropertyName("frequency_penalty")]
    public double FrequencyPenalty { get; } = frequencyPenalty;

    [JsonPropertyName("presence_penalty")]
    public int PresencePenalty { get; } = presencePenalty;
}