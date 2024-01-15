using FluentMigrator;

namespace AiKamu;

[Migration(2)]
public class _002_Conversations_AddGptModel : Migration
{
    public override void Down()
    {
        Delete.Column("Model").FromTable("Conversations");
    }

    public override void Up()
    {
        Alter.Table("Conversations")
            .AddColumn("Model").AsString().Nullable();
    }
}
