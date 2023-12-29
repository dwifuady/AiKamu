using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using AiKamu.Commands;
using AiKamu.Common;
using Microsoft.Extensions.Options;

namespace AiKamu.Bot;

public sealed class BotService(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordBotConfig> discordBotConfig) : IHostedService
{
    private const int maxMessageLength = 1990; //max lenght is 2000, but we reduce this so we can add something like (1/3) prefix on every message
    private DiscordSocketClient? _client;
    private DiscordBotConfig? _botConfig;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _botConfig = discordBotConfig.Value;

        if (string.IsNullOrWhiteSpace(_botConfig.Token))
        {
            Log.Error("Discord Token is Empty. Please specify on the settings DiscordBotConfig section");
            return;
        }

        _client = new DiscordSocketClient(GetDiscordSocketConfig());
        _client.Log += LogAsync;

        await _client.LoginAsync(TokenType.Bot, _botConfig.Token);
        await _client.StartAsync();

        //_client.InteractionCreated += InteractionCreatedAsync;
        _client.Ready += ClientReady;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _client.MessageReceived += MessageReceivedAsync;

        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            await _client.StopAsync();
        }

        Log.Information("Service stopped");
    }

    private static DiscordSocketConfig GetDiscordSocketConfig() => new()
    {
        GatewayIntents = GatewayIntents.DirectMessages |
                                   GatewayIntents.MessageContent |
                                   GatewayIntents.GuildMembers |
                                   GatewayIntents.GuildMessages |
                                   GatewayIntents.Guilds |
                                   GatewayIntents.GuildIntegrations,
        AlwaysDownloadUsers = false
    };

    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }

    public async Task ClientReady()
    {
        if (_client == null)
            return;

        try
        {
            await CreateDefaultGuildCommand();
        }
        catch (HttpException exception)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Log.Error(exception, json);
        }
    }

    private async Task SlashCommandHandler(SocketSlashCommand slashCommand)
    {
        if (_client is null)
        {
            return;
        }

        // Getting command based on slash command
        var command = serviceProvider.GetKeyedService<ICommand>(slashCommand.Data.Name);

        if (command == null)
        {
            await slashCommand.FollowupAsync($"Can't process your command. Can't find Command handler with name {slashCommand.Data.Name} ", ephemeral: true);
            return;
        }

        bool privateReply = command.IsPrivateResponse(new CommandArgs(slashCommand.Data));
        try
        {
            // Thinking mode
            await slashCommand.DeferAsync(ephemeral: privateReply);

            var response = await command.GetResponseAsync(_client, new CommandArgs(slashCommand.Data));
            await Reply(slashCommand, privateReply, response);
        }
        catch (Exception ex)
        {
            await slashCommand.FollowupAsync($"Can't process your command. Exception occured while processing {slashCommand.Data.Name}. {ex.Message} ", ephemeral: true);
        }
    }

    private Task MessageReceivedAsync(SocketMessage message)
    {
        // The bot should never respond to itself.
        if (message.Author.Id == _client?.CurrentUser.Id)
            return Task.CompletedTask;

        if (message.Channel is SocketGuildChannel socketGuildChannel)
        {
            Log.Information("Received a '{messageText}' message from {user} in chat {ServerName} > {ChannelName}.", message.Content, message.Author.Username, socketGuildChannel.Guild.Name, message.Channel.Name);
        }
        else
        {
            Log.Information("Received a '{messageText}' message from {user} in chat {ChannelName}.", message.Content, message.Author.Username, message.Channel.Name);
        }
        return Task.CompletedTask;
    }

    private async Task Reply(SocketSlashCommand slashCommand, bool privateReply, IResponse response)
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


    /// <summary>
    /// This is to create a Default Command to manage Guild Command
    /// I don't know what's the best way to manage command, so I will put this here, and manage the guild command manually using this command 
    /// </summary>
    /// <returns></returns>
    private async Task CreateDefaultGuildCommand()
    {
        if (_client == null)
            return;

        if (_botConfig == null || _botConfig?.BotManagementServerGuild == 0)
        {
            return;
        }

        // personal server guild
        var guild = _client.GetGuild(_botConfig!.BotManagementServerGuild);

        var guildCommand = new SlashCommandBuilder()
            .WithName(SlashCommandConstants.CommandNameManageCommand)
            .WithDescription("Add Guild Command to Specific Guild")
            .AddOption(
                new SlashCommandOptionBuilder()
                    .WithName(SlashCommandConstants.OptionNameCommandAction)
                    .WithDescription("Action")
                    .WithRequired(true)
                    .AddChoice(SlashCommandConstants.OptionChoiceAdd, SlashCommandConstants.OptionChoiceAdd)
                    .AddChoice(SlashCommandConstants.OptionChoiceDelete, SlashCommandConstants.OptionChoiceDelete)
                    .WithType(ApplicationCommandOptionType.String)
                )
            .AddOption(
                new SlashCommandOptionBuilder()
                    .WithName(SlashCommandConstants.OptionNameGuildId)
                    .WithDescription("GuildId")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
                )
            .AddOption(
                new SlashCommandOptionBuilder()
                    .WithName(SlashCommandConstants.OptionNameCommandName)
                    .WithDescription("CommandName")
                    .WithRequired(true)
                    .AddChoice(SlashCommandConstants.CommandNameAI, SlashCommandConstants.CommandNameAI)
                    .AddChoice(SlashCommandConstants.CommandNameSicepat, SlashCommandConstants.CommandNameSicepat)
                    .WithType(ApplicationCommandOptionType.String));

        await guild.CreateApplicationCommandAsync(guildCommand.Build());
    }
}
