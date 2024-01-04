using AiKamu.Bot;
using AiKamu.Common;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;

namespace AiKamu.Commands.CommandManagement;

public class CommandManagement(IOptions<DiscordBotConfig> options) : ICommand
{
    private readonly DiscordBotConfig _botConfig = options.Value;

    public bool IsPrivateResponse(CommandArgs commandArgs)
    {
        return true;
    }

    public async Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, CommandArgs commandArgs)
    {
        var guildId = Convert.ToUInt64(commandArgs.Args[SlashCommandConstants.OptionNameGuildId]);
        
        if (guildId <= 0)
        {
            Log.Warning("GuildId {GuildId} is an invalid guild", guildId);
            return new TextResponse(false, $"Guild {guildId} is an invalid guild");
        }

        var guild = discordSocketClient.GetGuild(guildId);

        if (guild is null)
        {
            Log.Warning("GuildId {GuildId} is an invalid guild", guildId);
            return new TextResponse(false, $"Guild {guildId} is an invalid guild");
        }

        var action = commandArgs.Args[SlashCommandConstants.OptionNameCommandAction] as string;
        
        if (commandArgs.Args[SlashCommandConstants.OptionNameCommandName] is not string commandName)
        {
            return new TextResponse(false, $"{SlashCommandConstants.OptionNameCommandName} is required.");
        }

        return action switch
        {
            SlashCommandConstants.OptionChoiceAdd => await AddCommand(commandName, guild),
            SlashCommandConstants.OptionChoiceDelete => await DeleteCommand(discordSocketClient, guild, commandName),
            _ => new TextResponse(false, $"Invalid action {action}"),
        };
    }

    private static async Task<IResponse> AddCommand(string commandName, SocketGuild guild)
    {
        var allCommands = await guild.GetApplicationCommandsAsync();
        Log.Information("Adding {CommandName} started", commandName);
        var slashCommandFound = SlashCommandBuilders.TryGetValue(commandName, out var slashCommandBuilder);
        
        if (slashCommandFound && slashCommandBuilder != null && !allCommands.Any(x => x.Name.Equals(commandName)))
        {
            Log.Information("Adding {CommandName} as a slash command", commandName);
            var result = await guild.CreateApplicationCommandAsync(slashCommandBuilder.Build());
            return new TextResponse(true, $"Command {result.Name} created at {result.CreatedAt} for {guild.Name}");
        }
        
        var messageCommandFound = MessageCommandBuilders.TryGetValue(commandName, out var messageCommandBuilder);
        if (messageCommandFound && messageCommandBuilder != null && !allCommands.Any(x => x.Name.Equals(commandName)))
        {
            Log.Information("Adding {CommandName} as a message command", commandName);
            var result = await guild.CreateApplicationCommandAsync(messageCommandBuilder.Build());
            return new TextResponse(true, $"Command {result.Name} created at {result.CreatedAt} for {guild.Name}");
        }

        Log.Information("{CommandName} not added because it's not available or already added ", commandName);

        return new TextResponse(true, $"Command {commandName} not available or already added");
    }

    private static async Task<IResponse> DeleteCommand(DiscordSocketClient discordSocketClient, SocketGuild guild, string commandName)
    {
        if (commandName.Equals(SlashCommandConstants.CommandNameManageCommand, StringComparison.InvariantCultureIgnoreCase))
        {
            return new TextResponse(false, $"{commandName} is a required command and can't be deleted");
        }

        var allCommands = await guild.GetApplicationCommandsAsync();
        
        var commandToBeDeleted = allCommands.FirstOrDefault(c => c.Name == commandName);

        if (commandToBeDeleted != null)
        {
            await commandToBeDeleted.DeleteAsync();
        }

        return new TextResponse(false, $"Command deleted");
    }

    private static Dictionary<string, SlashCommandBuilder> SlashCommandBuilders => new()
    {
        {
            SlashCommandConstants.CommandNameAI,
            new SlashCommandBuilder()
                .WithName(SlashCommandConstants.CommandNameAI)
                .WithDescription("Talk or ask to draw an image to ChatGPT. This command does not support conversation.")
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.OptionNameMessage)
                        .WithDescription("What do you want to ask?")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String))
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.OptionNameEphemeral)
                        .WithDescription("Show the reply only to you? Default is true")
                        .WithRequired(false)
                        .WithType(ApplicationCommandOptionType.Boolean))
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.OptionNameLanguageModel)
                        .WithDescription("Language Model. Default is 3.5 Turbo")
                        .WithRequired(false)
                        .AddChoice(SlashCommandConstants.OptionChoice35TurboDesc, SlashCommandConstants.OptionChoice35Turbo)
                        .AddChoice(SlashCommandConstants.OptionChoice4TurboDesc, SlashCommandConstants.OptionChoice4Turbo)
                        .WithType(ApplicationCommandOptionType.String))
        },
        {
            SlashCommandConstants.CommandNameSicepat,
            new SlashCommandBuilder()
                .WithName(SlashCommandConstants.CommandNameSicepat)
                .WithDescription("Track the delivery from SiCepat")
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.OptionNameTrackingNumber)
                        .WithDescription("Tracking Number")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String))
        },
        {
            SlashCommandConstants.CommandNameManageCommand,
            new SlashCommandBuilder()
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
                        .WithType(ApplicationCommandOptionType.String))
        }
    };

    private static Dictionary<string, MessageCommandBuilder> MessageCommandBuilders => new()
    {
        {
            SlashCommandConstants.CommandNameTranslateId,
            new MessageCommandBuilder()
                .WithName(SlashCommandConstants.CommandNameTranslateId)
        },
        {
            SlashCommandConstants.CommandNameTranslateEn,
            new MessageCommandBuilder()
                .WithName(SlashCommandConstants.CommandNameTranslateEn)
        }
    };
}
