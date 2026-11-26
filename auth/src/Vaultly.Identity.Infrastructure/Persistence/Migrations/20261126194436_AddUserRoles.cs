using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vaultly.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "user");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "AuthorizationRequests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "CodeChallenge",
                table: "AuthorizationRequests",
                type: "character varying(43)",
                maxLength: 43,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(43)",
                oldMaxLength: 43);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // Admin console sign-ins have neither; they live minutes and cannot be expressed in the old schema.
            migrationBuilder.Sql("DELETE FROM \"AuthorizationRequests\" WHERE \"State\" IS NULL OR \"CodeChallenge\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "AuthorizationRequests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodeChallenge",
                table: "AuthorizationRequests",
                type: "character varying(43)",
                maxLength: 43,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(43)",
                oldMaxLength: 43,
                oldNullable: true);
        }
    }
}
