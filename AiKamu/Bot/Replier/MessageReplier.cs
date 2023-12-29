using AiKamu.Commands;
using AiKamu.Common;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace AiKamu.Bot.Replier;

public class MessageReplier(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory) : IMessageReplier
{
    private const int maxMessageLength = 1990; //max lenght is 2000, but we reduce this so we can add something like (1/3) prefix on every message
    public async Task Reply(SocketMessage message, IResponse response)
    {
        switch (response)
        {
            case ITextResponse textResponse:
                {
                    var messages = textResponse.Message?.Chunk(maxMessageLength)
                            .Select(s => new string(s))
                            .ToList();

                    var messagesCount = messages?.Count;

                    if (messages == null || messagesCount == 0)
                    {
                        break;
                    }

                    int currentMessage = 1;
                    foreach (var responseMessage in messages)
                    {
                        var prefix = string.Empty;
                        if (messagesCount > 1)
                        {
                            prefix = $"({currentMessage}/{messagesCount}) {Environment.NewLine}";
                        }
                        var sentMessage = await message.Channel.SendMessageAsync(prefix + responseMessage, messageReference: new MessageReference(messageId: message.Id));
                        currentMessage++;

                        await SaveBotReplyMessage(sentMessage.Id, message.Id, textResponse);
                    }
                    break;
                }
            case IFileResponse or IImageResponse:
                {
                    var sourceUrl = string.Empty;
                    var caption = string.Empty;
                    if (response is IFileResponse fileResponse)
                    {
                        sourceUrl = fileResponse.SourceUrl;
                        caption = fileResponse.Caption;
                    }
                    else if (response is IImageResponse imageResponse)
                    {
                        sourceUrl = imageResponse.ImageUrl;
                        caption = imageResponse.Caption;
                    }
                    if (!string.IsNullOrWhiteSpace(sourceUrl))
                    {
                        if (!await SendFile(sourceUrl, caption, message))
                        {
                            await message.Channel.SendMessageAsync("Error generating your image, please try again later.", messageReference: new MessageReference(messageId: message.Id));
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Error generating your image, please try again later.", messageReference: new MessageReference(messageId: message.Id));
                    }
                    break;
                }
        }
    }

    private async Task<bool> SendFile(string fileUrl, string? caption, SocketMessage message)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var httpResponse = await httpClient.GetAsync(fileUrl);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;

            if (string.IsNullOrWhiteSpace(contentType) || (!contentType.StartsWith("image/") && !contentType.StartsWith("video/")))
            {
                Log.Error($"Response from {fileUrl} was not an image or video");
                return false;
            }

            var fileStream = await httpResponse.Content.ReadAsStreamAsync();
            var fileExtension = FileHelper.GetFileExtension(contentType);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            await message.Channel.SendFileAsync(new FileAttachment(fileStream, fileName), text: caption, messageReference: new MessageReference(messageId: message.Id));

            return true;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, $"Failed to download file from {fileUrl}");
            throw;
        }
    }

    private async Task SaveBotReplyMessage(ulong id, ulong replyToId, IResponse response)
    {
        if (response is not TextResponse textResponse)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();

        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversationId = appDbContext.MessageChains.FirstOrDefault(m => m.Id == replyToId)?.ConversationId;

        if (conversationId.HasValue)
        {
            var messageChain = new MessageChain
            {
                Id = id,
                ConversationId = conversationId.Value,
                Content = textResponse.Message,
                Role = RoleConstants.RoleAssistant
            };

            appDbContext.MessageChains.Add(messageChain);

            await appDbContext.SaveChangesAsync();
        }
    }
}
