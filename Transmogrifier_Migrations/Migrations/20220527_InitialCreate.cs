using FluentMigrator;

namespace Greetings_MySqlMigrations.Migrations;

[Migration(1)]
public class SqlInitialCreate : Migration 
{
    public override void Up()
    {
        var person = Create.Table("Person")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString().Unique()
            .WithColumn("TimeStamp").AsDateTime().Nullable().WithDefault(SystemMethods.CurrentDateTime);
        
        var transmogrification = Create.Table("Transmogrification")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Description").AsString()
            .WithColumn("Recipient_Id").AsInt32();
    }

    public override void Down()
    {
        Delete.Table("Transmogrification");
        Delete.Table("Person");
    }    
}
