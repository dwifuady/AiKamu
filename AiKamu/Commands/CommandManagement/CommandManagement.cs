using AiKamu.Bot;
using AiKamu.Common;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace AiKamu.Commands.CommandManagement;

public class CommandManagement(IOptions<DiscordBotConfig> options) : ICommand
{
    private readonly DiscordBotConfig _botConfig = options.Value;

    public bool IsPrivateResponse(SocketSlashCommandData data)
    {
        return true;
    }

    public async Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, SocketSlashCommandData data)
    {
        _ = ulong.TryParse(data?.Options?.FirstOrDefault(x => x.Name == SlashCommandConstants.OptionNameGuildId)?.Value as string, out var guildId);
        
        if (guildId <= 0)
        {
            return new TextResponse(false, $"Guild {guildId} is an invalid guild");
        }

        var guild = discordSocketClient.GetGuild(guildId);

        if (guild is null)
        {
            return new TextResponse(false, $"Guild {guildId} is an invalid guild");
        }

        var action = data?.Options?.FirstOrDefault(x => x.Name == SlashCommandConstants.OptionNameCommandAction)?.Value as string;
        
        if (data?.Options?.FirstOrDefault(x => x.Name == SlashCommandConstants.OptionNameCommandName)?.Value is not string commandName)
        {
            return new TextResponse(false, $"{SlashCommandConstants.OptionNameCommandName} is required.");
        }

        switch (action)
        {
            case SlashCommandConstants.OptionChoiceAdd:
                return await AddCommand(commandName, guild);
            case SlashCommandConstants.OptionChoiceDelete:
                return await DeleteCommand(discordSocketClient, guild, commandName);
            default:
                return new TextResponse(false, $"Invalid action {action}");
        }
    }

    private static async Task<IResponse> AddCommand(string commandName, SocketGuild guild)
    {
        var guildCommand = SlashCommandBuilders[commandName];

        var result = await guild.CreateApplicationCommandAsync(guildCommand.Build());

        return new TextResponse(false, $"Command {result.Name} created at {result.CreatedAt} for {guild.Name}");
    }

    private static async Task<IResponse> DeleteCommand(DiscordSocketClient discordSocketClient, SocketGuild guild, string commandName)
    {
        // await guild.DeleteApplicationCommandsAsync();
        // await ReAddDefaultCommand(discordSocketClient, guild);
        // return new TextResponse(false, $"Command deleted");

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

    /// <summary>
    /// re-add the default builder to admin server if all command in admin server deleted
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
    private async Task ReAddDefaultCommand(DiscordSocketClient discordSocketClient, SocketGuild guild)
    {
        if (_botConfig.BotManagementServerGuild == guild.Id)
        {
            await guild.CreateApplicationCommandAsync(DefaultSlashCommandBuilder.Build());
        }
    }

    private static Dictionary<string, SlashCommandBuilder> SlashCommandBuilders => new()
    {
        {
            SlashCommandConstants.CommandNameAI,
            new SlashCommandBuilder()
                .WithName(SlashCommandConstants.CommandNameAI)
                .WithDescription("Talk to ChatGPT. This command does not support conversation.")
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.OptionNamePrompt)
                        .WithDescription("What do you want to ask?")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.String))
                .AddOption(
                    new SlashCommandOptionBuilder()
                        .WithName(SlashCommandConstants.OptionNameEphemeral)
                        .WithDescription("Show the reply only to you?")
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
        }
    };

    private static SlashCommandBuilder DefaultSlashCommandBuilder => new SlashCommandBuilder()
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

}
