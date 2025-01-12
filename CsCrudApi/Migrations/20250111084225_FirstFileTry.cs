using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class FirstFileTry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comentario_comentario_ParentCommentaryId1",
                table: "comentario");

            migrationBuilder.DropForeignKey(
                name: "FK_comentario_post_PostGuid",
                table: "comentario");

            migrationBuilder.DropForeignKey(
                name: "FK_comentario_usuario_UserId1",
                table: "comentario");

            migrationBuilder.DropIndex(
                name: "IX_comentario_ParentCommentaryId1",
                table: "comentario");

            migrationBuilder.DropIndex(
                name: "IX_comentario_PostGuid",
                table: "comentario");

            migrationBuilder.DropIndex(
                name: "IX_comentario_UserId1",
                table: "comentario");

            migrationBuilder.DropColumn(
                name: "ParentCommentaryId1",
                table: "comentario");

            migrationBuilder.DropColumn(
                name: "PostGuid",
                table: "comentario");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "comentario");

            migrationBuilder.RenameColumn(
                name: "Curso",
                table: "usuario",
                newName: "CursoId");

            migrationBuilder.AddColumn<string>(
                name: "url_foto_perfil",
                table: "usuario",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "url_foto_perfil",
                table: "usuario");

            migrationBuilder.RenameColumn(
                name: "CursoId",
                table: "usuario",
                newName: "Curso");

            migrationBuilder.AddColumn<int>(
                name: "ParentCommentaryId1",
                table: "comentario",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PostGuid",
                table: "comentario",
                type: "varchar(32)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "comentario",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_comentario_ParentCommentaryId1",
                table: "comentario",
                column: "ParentCommentaryId1");

            migrationBuilder.CreateIndex(
                name: "IX_comentario_PostGuid",
                table: "comentario",
                column: "PostGuid");

            migrationBuilder.CreateIndex(
                name: "IX_comentario_UserId1",
                table: "comentario",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_comentario_comentario_ParentCommentaryId1",
                table: "comentario",
                column: "ParentCommentaryId1",
                principalTable: "comentario",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comentario_post_PostGuid",
                table: "comentario",
                column: "PostGuid",
                principalTable: "post",
                principalColumn: "guid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comentario_usuario_UserId1",
                table: "comentario",
                column: "UserId1",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
