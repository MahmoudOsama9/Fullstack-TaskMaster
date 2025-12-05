using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class invitedToInviter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitedId",
                table: "ProjectInvitations");

            migrationBuilder.RenameColumn(
                name: "InvitedEmail",
                table: "ProjectInvitations",
                newName: "InviterEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InviterEmail",
                table: "ProjectInvitations",
                newName: "InvitedEmail");

            migrationBuilder.AddColumn<int>(
                name: "InvitedId",
                table: "ProjectInvitations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
