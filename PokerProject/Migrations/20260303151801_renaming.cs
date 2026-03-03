using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerProject.Migrations
{
    /// <inheritdoc />
    public partial class renaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Users_KnockedOutUserId",
                table: "Scores");

            migrationBuilder.RenameColumn(
                name: "KnockedOutUserId",
                table: "Scores",
                newName: "VictimUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Scores_KnockedOutUserId",
                table: "Scores",
                newName: "IX_Scores_VictimUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Users_VictimUserId",
                table: "Scores",
                column: "VictimUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Users_VictimUserId",
                table: "Scores");

            migrationBuilder.RenameColumn(
                name: "VictimUserId",
                table: "Scores",
                newName: "KnockedOutUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Scores_VictimUserId",
                table: "Scores",
                newName: "IX_Scores_KnockedOutUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Users_KnockedOutUserId",
                table: "Scores",
                column: "KnockedOutUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
