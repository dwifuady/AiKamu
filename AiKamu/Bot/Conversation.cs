namespace AiKamu.Bot;

public class Conversation
{
    public int Id { get; set; }
    public string? Command { get; set; }
    public ICollection<MessageChain>? MessageChains { get; set; }
}

public class MessageChain
{
    public ulong Id { get; set; }
    public string? Content { get; set; }
    public ulong? ReplyToId { get; set; }
    public string Role { get; set; } = null!;
    public ICollection<MessageAttachment>? Attachments { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
}

public class MessageAttachment
{
    public ulong Id { get; set; }
    public string? Url { get; set; }
    
    public ulong MessageId { get; set; }
    public MessageChain MessageChain { get; set; } = null!;
}