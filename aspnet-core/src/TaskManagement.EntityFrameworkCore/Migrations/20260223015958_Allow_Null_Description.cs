using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Allow_Null_Description : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppTasks",
                type: "nvarchar(max)",
                nullable: true,
                collation: "Vietnamese_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldCollation: "Vietnamese_CI_AI");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                collation: "Vietnamese_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldCollation: "Vietnamese_CI_AI");
        }
    }
}
