﻿using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using AiKamu.Commands;
using AiKamu.Common;
using Microsoft.Extensions.Options;
using AiKamu.Bot.Replier;

namespace AiKamu.Bot;

public sealed class BotService(
    IServiceProvider serviceProvider,
    IOptions<DiscordBotConfig> discordBotConfig,
    ISlashCommandReplier slashCommandReplier,
    IMessageReplier messageReplier,
    IServiceScopeFactory scopeFactory) : IHostedService
{
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
        _client.MessageCommandExecuted += MessageCommandHandler;

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

        var commandArgs = new CommandArgs(slashCommand.Data.Options.ToDictionary(o => o.Name, o => o.Value), slashCommand.Data.Name);
        Log.Information("Command {commandName} called by {userName}. CommandArgs {args}", commandArgs.CommandName, slashCommand.User.Username, JsonConvert.SerializeObject(commandArgs));

        // Getting command based on slash command
        var command = serviceProvider.GetKeyedService<ICommand>(commandArgs.CommandName);

        if (command == null)
        {
            await slashCommand.FollowupAsync($"Can't process your command. Can't find Command handler with name {slashCommand.Data.Name} ", ephemeral: true);
            return;
        }

        bool privateReply = command.IsPrivateResponse(commandArgs);
        
        // Thinking mode
        await slashCommand.DeferAsync(ephemeral: privateReply);
        try
        {
            // save conversation if it's not a private reply
            if (!privateReply)
            {
                await SaveInitialConversation(slashCommand.Id, commandArgs);
            }

            var response = await command.GetResponseAsync(_client, commandArgs);

            await slashCommandReplier.Reply(slashCommand, privateReply, response);
        }
        catch (Exception ex)
        {
            await slashCommand.RespondAsync($"Can't process your command. Exception occured while processing {slashCommand.Data.Name}. {ex.Message} ", ephemeral: true);
        }
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (_client is null)
        {
            return;
        }

        // The bot should never respond to itself.
        if (message.Author.Id == _client?.CurrentUser.Id)
            return;

        if (message.Channel is SocketGuildChannel socketGuildChannel)
        {
            Log.Information("Received a '{messageText}' message from {user} in chat {ServerName} > {ChannelName}.", message.Content, message.Author.Username, socketGuildChannel.Guild.Name, message.Channel.Name);
        }
        else
        {
            Log.Information("Received a '{messageText}' message from {user} in chat {ChannelName}.", message.Content, message.Author.Username, message.Channel.Name);
        }

        if (message.Reference?.MessageId is not null && await message.Channel.GetMessageAsync(message.Reference.MessageId.Value) is { } repliedMessage)
        {
            await HandleConversation(message, repliedMessage);
        }
        else
        {
            var commandArgs = message.Content.Parse<CommandArgs>();
            await HandleFirstCallCommand(message, commandArgs);
        }
    }

    /// <summary>
    /// [WIP] MessageCommand feature
    /// </summary>
    /// <param name="socketCommand"></param>
    /// <returns></returns>
    public async Task MessageCommandHandler(SocketMessageCommand socketCommand)
    {
        if (_client is null)
        {
            return;
        }
        
        var args = new Dictionary<string, object>
        {
            { SlashCommandConstants.OptionNameMessage, socketCommand.Data.Message.CleanContent}
        };

        var commandArgs = new CommandArgs(args, socketCommand.Data.Name);
        Log.Information("Command {commandName} called by {userName}. CommandArgs {args}", commandArgs.CommandName, socketCommand.User.Username, JsonConvert.SerializeObject(commandArgs));

        // Getting command based on slash command
        var command = serviceProvider.GetKeyedService<ICommand>(commandArgs.CommandName);

        if (command == null)
        {
            await socketCommand.FollowupAsync($"Can't process your command. Can't find Command handler with name {socketCommand.Data.Name} ", ephemeral: true);
            return;
        }

        bool privateReply = command.IsPrivateResponse(commandArgs);
        
        // Thinking mode
        await socketCommand.DeferAsync(ephemeral: privateReply);
        try
        {
            // save conversation if it's not a private reply
            if (!privateReply)
            {
                await SaveInitialConversation(socketCommand.Id, commandArgs);
            }

            var response = await command.GetResponseAsync(_client, commandArgs);

            switch (response)
            {
                case ITextResponse textResponse:
                    {
                        var messages = textResponse.Message?.Chunk(1900)
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
                            var sentMessage = await socketCommand.FollowupAsync(prefix + responseMessage, ephemeral: privateReply);
                            currentMessage++;
                        }
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            await socketCommand.RespondAsync($"Can't process your command. Exception occured while processing {socketCommand.Data.Name}. {ex.Message} ", ephemeral: true);
        }
    }

    private async Task HandleFirstCallCommand(SocketMessage message, CommandArgs commandArgs)
    {
        // Getting command based on prefix
        var command = serviceProvider.GetKeyedService<ICommand>(commandArgs.CommandName);

        if (command == null)
        {
            return;
        }

        try
        {
            await SaveInitialConversation(message.Id, commandArgs);

            var response = await command.GetResponseAsync(_client!, commandArgs);
            await messageReplier.Reply(message, response);

        }
        catch (Exception ex)
        {
            await message.Channel.SendMessageAsync($"Can't process your command. Exception occured while processing your request. {ex.Message} ", messageReference: new MessageReference(messageId: message.Id));
        }
    }

    private async Task HandleConversation(SocketMessage message, IMessage repliedMessage)
    {
        using var scope = scopeFactory.CreateScope();

        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messageChain = appDbContext.MessageChains.SingleOrDefault(r => r.Id == repliedMessage.Id);

        // Ignore if it's not a reply to the bot
        if (messageChain == null || messageChain.Role == RoleConstants.RoleUser)
        {
            return;
        }

        var conversation = appDbContext.Conversations.SingleOrDefault(c => c.Id == messageChain.ConversationId);

        if (conversation == null)
        {
            return;
        }

        var chains = appDbContext
            .MessageChains
            .Where(m => m.ConversationId == conversation.Id)
            .ToList()
            .OrderBy(m => m.Id)
            .Select(o => new KeyValuePair<string, string>(o.Role, o.Content!)).ToList();

        chains.Add(new(RoleConstants.RoleUser, message.CleanContent));

        var args = new Dictionary<string, object>
        {
            { SlashCommandConstants.OptionNameConversation, chains}
        };

        var commandArgs = new CommandArgs(args, conversation.Command!);

        // Getting command based on prefix
        var command = serviceProvider.GetKeyedService<ICommand>(commandArgs.CommandName);

        if (command == null)
        {
            return;
        }

        try
        {
            var newMessageChain = new MessageChain
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                Content = message.CleanContent,
                Role = RoleConstants.RoleUser
            };

            appDbContext.MessageChains.Add(newMessageChain);

            await appDbContext.SaveChangesAsync();

            var response = await command.GetResponseAsync(_client!, commandArgs);
            await messageReplier.Reply(message, response);
        }
        catch (Exception ex)
        {
            await message.Channel.SendMessageAsync($"Can't process your command. Exception occured while processing your request. {ex.Message} ", messageReference: new MessageReference(messageId: message.Id));
        }
    }

    private async Task SaveInitialConversation(ulong id, CommandArgs commandArgs)
    {
        using var scope = scopeFactory.CreateScope();

        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = new Conversation
        {
            Command = commandArgs.CommandName
        };

        appDbContext.Conversations.Add(conversation);
        await appDbContext.SaveChangesAsync();

        var messageChain = new MessageChain
        {
            Id = id,
            ConversationId = conversation.Id,
            Content = commandArgs.Args[SlashCommandConstants.OptionNameMessage] as string,
            Role = RoleConstants.RoleUser
        };

        appDbContext.MessageChains.Add(messageChain);

        await appDbContext.SaveChangesAsync();
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
                    .AddChoice(SlashCommandConstants.CommandNameTranslateId, SlashCommandConstants.CommandNameTranslateId)
                    .AddChoice(SlashCommandConstants.CommandNameTranslateEn, SlashCommandConstants.CommandNameTranslateEn)
                    .WithType(ApplicationCommandOptionType.String));

        await guild.CreateApplicationCommandAsync(guildCommand.Build());

        // Context Menu Command
        var guildMessageCommandTranslateId = new MessageCommandBuilder();
        guildMessageCommandTranslateId.WithName(SlashCommandConstants.CommandNameTranslateId);

        var guildMessageCommandTranslateEn = new MessageCommandBuilder();
        guildMessageCommandTranslateEn.WithName(SlashCommandConstants.CommandNameTranslateEn);

        await guild.CreateApplicationCommandAsync(guildMessageCommandTranslateId.Build());
        await guild.CreateApplicationCommandAsync(guildMessageCommandTranslateEn.Build());

        /*
        await guild.BulkOverwriteApplicationCommandAsync(
        [
            guildCommand.Build(),
            guildMessageCommandTranslateId.Build(),
            guildMessageCommandTranslateEn.Build()
        ]);
        */
    }
}
