using AiKamu.Commands;
using AiKamu.Common;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace AiKamu.Bot.Replier;

public class SlashCommandReplier(
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory) : ISlashCommandReplier
{
    private const int maxMessageLength = 1990; //max lenght is 2000, but we reduce this so we can add something like (1/3) prefix on every message

    public async Task Reply(SocketSlashCommand slashCommand, bool privateReply, IResponse response)
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
                        var sentMessage = await slashCommand.FollowupAsync(prefix + responseMessage, ephemeral: privateReply);
                        currentMessage++;

                        if (!privateReply)
                        {
                            await SaveBotReplyMessage(sentMessage.Id, slashCommand.Id, textResponse);
                        }
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
                        if (!await SendFile(sourceUrl, caption, privateReply, slashCommand))
                        {
                            await slashCommand.FollowupAsync("Error generating your image, please try again later.", ephemeral: privateReply);
                        }
                    }
                    else
                    {
                        await slashCommand.FollowupAsync("Error generating your image, please try again later.", ephemeral: privateReply);
                    }
                    break;
                }
        }
    }

    private async Task<bool> SendFile(string fileUrl, string? caption, bool privateReply, SocketSlashCommand message)
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

            await message.FollowupWithFileAsync(new FileAttachment(fileStream, fileName), text: caption, ephemeral: privateReply);

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
                Role = RoleConstants.RoleAssistant,
                ReplyToId = replyToId
            };

            appDbContext.MessageChains.Add(messageChain);

            await appDbContext.SaveChangesAsync();
        }
    }
}
