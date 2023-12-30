using FluentMigrator;

namespace AiKamu.Migrations;

[Migration(1)]
public class _001_InitialMigration : Migration
{
    public override void Up()
    {
        Create.Table("Conversations")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Command").AsString();

        Create.Table("MessageChains")
            .WithColumn("Id").AsInt64().PrimaryKey()
            .WithColumn("Role").AsString()
            .WithColumn("Content").AsString()
            .WithColumn("ReplyToId").AsInt64().Nullable()
            .WithColumn("ConversationId").AsInt32();

        Create.Table("MessageAttachments")
            .WithColumn("Id").AsInt64().PrimaryKey()
            .WithColumn("MessageId").AsInt64()
            .WithColumn("Url").AsString();
    }
    public override void Down()
    {
        Delete.Table("Conversations");
        Delete.Table("MessageChains");
        Delete.Table("MessageAttachments");
    }
}