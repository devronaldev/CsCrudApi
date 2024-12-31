using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class VirtualVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_post_area",
                table: "post",
                column: "area");

            migrationBuilder.AddForeignKey(
                name: "FK_post_area_area",
                table: "post",
                column: "area",
                principalTable: "area",
                principalColumn: "id_area",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_post_area_area",
                table: "post");

            migrationBuilder.DropIndex(
                name: "IX_post_area",
                table: "post");
        }
    }
}
