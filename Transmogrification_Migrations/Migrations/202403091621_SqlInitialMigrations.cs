using FluentMigrator;

namespace Transmogrification_Migrations.Migrations;

[Migration(1)]
public class SqlInitialMigrations : Migration
{
    public override void Up()
    {
        Create.Table("Transmogrification")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("Name").AsString()
            .WithColumn("Transformation").AsString();
    }

    public override void Down()
    {
        Delete.Table("Transmogrification");
    }
}
