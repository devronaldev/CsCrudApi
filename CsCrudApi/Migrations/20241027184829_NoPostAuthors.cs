using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class NoPostAuthors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_autores");

            migrationBuilder.AddColumn<string>(
                name: "descricao_titulo",
                table: "post",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "id_usuario",
                table: "post",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_post_id_usuario",
                table: "post",
                column: "id_usuario");

            migrationBuilder.AddForeignKey(
                name: "FK_post_usuario_id_usuario",
                table: "post",
                column: "id_usuario",
                principalTable: "usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_post_usuario_id_usuario",
                table: "post");

            migrationBuilder.DropIndex(
                name: "IX_post_id_usuario",
                table: "post");

            migrationBuilder.DropColumn(
                name: "descricao_titulo",
                table: "post");

            migrationBuilder.DropColumn(
                name: "id_usuario",
                table: "post");

            migrationBuilder.CreateTable(
                name: "post_autores",
                columns: table => new
                {
                    cd_usuario = table.Column<int>(type: "int", nullable: false),
                    guid_post = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_autores", x => new { x.cd_usuario, x.guid_post });
                    table.ForeignKey(
                        name: "FK_post_autores_post_guid_post",
                        column: x => x.guid_post,
                        principalTable: "post",
                        principalColumn: "guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_autores_usuario_cd_usuario",
                        column: x => x.cd_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_post_autores_guid_post",
                table: "post_autores",
                column: "guid_post");
        }
    }
}
