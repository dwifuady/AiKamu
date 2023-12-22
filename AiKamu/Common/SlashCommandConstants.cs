namespace AiKamu.Common;

public static class SlashCommandConstants
{
    public const string CommandNameHello = "hello";

    // Default Command
    public const string CommandNameManageCommand = "manage-command";
    public const string CommandNameAddCommand = "add-command";
    public const string OptionNameCommandAction = "action";
    public const string OptionNameGuildId = "guild-id";
    public const string OptionNameCommandName = "command-name";
    public const string OptionChoiceAdd = "Add";
    public const string OptionChoiceDelete = "Delete";


    // OpenAi
    public const string CommandNameAI = "ai";
    public const string OptionNamePrompt = "message";
    public const string OptionNameEphemeral = "private";
    public const string OptionNameLanguageModel = "model";
    public const string OptionChoice35Turbo = "gpt-3.5-turbo-1106";
    public const string OptionChoice35TurboDesc = "GPT-3.5 Turbo. Faster";
    public const string OptionChoice4Turbo = "gpt-4-1106-preview";
    public const string OptionChoice4TurboDesc = "GPT-4 Turbo. Fresher knowledge and the broadest set of capabilities";

    // SiCepat
    public const string CommandNameSicepat = "sicepat";
    public const string OptionNameTrackingNumber = "tracking-number";

}
