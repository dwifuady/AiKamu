using Microsoft.EntityFrameworkCore;

namespace AiKamu.Bot;

public class AppDbContext : DbContext
{
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<MessageChain> MessageChains { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=app.db;Cache=Shared");
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>()
            .HasMany(e => e.MessageChains)
            .WithOne(e => e.Conversation)
            .HasForeignKey(e => e.ConversationId)
            .HasPrincipalKey(e => e.Id);

        modelBuilder.Entity<MessageChain>()
            .HasMany(e => e.Attachments)
            .WithOne(e => e.MessageChain)
            .HasForeignKey(e => e.MessageId)
            .HasPrincipalKey(e => e.Id);
    }
}
