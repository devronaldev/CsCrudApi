using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class NoParenthood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comentario_comentario_cd_comentario_pai",
                table: "comentario");

            migrationBuilder.DropIndex(
                name: "IX_comentario_cd_comentario_pai",
                table: "comentario");

            migrationBuilder.DropColumn(
                name: "cd_comentario_pai",
                table: "comentario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cd_comentario_pai",
                table: "comentario",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_comentario_cd_comentario_pai",
                table: "comentario",
                column: "cd_comentario_pai");

            migrationBuilder.AddForeignKey(
                name: "FK_comentario_comentario_cd_comentario_pai",
                table: "comentario",
                column: "cd_comentario_pai",
                principalTable: "comentario",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
