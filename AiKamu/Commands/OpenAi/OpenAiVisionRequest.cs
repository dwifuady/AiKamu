using System.Text.Json.Serialization;

namespace AiKamu.Commands.OpenAi;

public class Content
{
    public Content(string type, string text)
    {
        Type = type;
        Text = text;
    }

    public Content(string type, ImageUrl imageUrl)
    {
        Type = type;
        ImageUrl = imageUrl;
    }

    [JsonPropertyName("type")]
    public string Type { get; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; }

    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImageUrl? ImageUrl { get; }
}

public class ImageUrl(string url)
{
    [JsonPropertyName("url")]
    public string Url { get; } = url;
}

public class Message(string role, List<Content> contents)
{
    [JsonPropertyName("role")]
    public string Role { get; } = role;

    [JsonPropertyName("content")]
    public List<Content> Content { get; } = contents;
}

public class OpenAiVisionRequest(string model, List<Message> messages, int maxTokens)
{
    [JsonPropertyName("model")]
    public string Model { get; } = model;

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; } = messages;

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; } = maxTokens;
}
