using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ConfigDataBlockPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column with SQL Server's 'bit' type, allowing nulls, and a default value of true.
            migrationBuilder.AddColumn<bool>(
                name: "IsOpenForPlayers",
                table: "ConfigurationsData",
                type: "bit",  // Use "bit" for SQL Server boolean values
                nullable: true,
                defaultValue: true);

            // Update existing rows: set the value to true (1) where the column is currently NULL.
            migrationBuilder.Sql("UPDATE ConfigurationsData SET IsOpenForPlayers = 1 WHERE IsOpenForPlayers IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpenForPlayers",
                table: "ConfigurationsData");
        }
    }
}
