using AiKamu.Common;
using System.Diagnostics.CodeAnalysis;

namespace AiKamu.Commands;

public class CommandArgs : IParsable<CommandArgs>
{
    private readonly Dictionary<string, object> _commandOptions;

    public CommandArgs(Dictionary<string, object> commandOptions, string commandName)
    {
        _commandOptions = commandOptions;
        CommandName = commandName;
    }

    /// <summary>
    /// This private constructor is used by Parse method
    /// </summary>
    /// <param name="commandName"></param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentException"></exception>
    private CommandArgs(string commandName, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        _commandOptions = new Dictionary<string, object>
        {
            { SlashCommandConstants.OptionNameMessage, message }
        };
        CommandName = commandName;
    }

    public string CommandName { get; private set; }
    public bool IsPrivateResponse 
    {
        get {
            _ = _commandOptions.TryGetValue(SlashCommandConstants.OptionNameEphemeral, out var privateResponseObj);
            if (privateResponseObj is bool privateResponse)
            {
                return privateResponse;
            }
            return true;
        }
    } 
    //(bool)(_commandOptions[SlashCommandConstants.OptionNameEphemeral] ?? true);
    public IReadOnlyDictionary<string, object> Args => _commandOptions;

    #region IParsable Implementation
    public static CommandArgs Parse(string s, IFormatProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new Exception("Message can't be empty");
        }

        string[] strings = s.Split([',',' ', ':']);
        if (strings.Length < 1)
        {
            throw new OverflowException($"Invalid input parameter {s}");
        }

        string command = strings[0].ToLower();
        var i = s.IndexOf(strings?.FirstOrDefault(x => x == " ") ?? " ", StringComparison.Ordinal) + 1;

        return new CommandArgs(command, s[i..]);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CommandArgs result)
    {
        result = null;
        if (s == null)
        {
            return false;
        }
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion
}