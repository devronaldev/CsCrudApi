using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class HotFixProd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "categoria",
                table: "post",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario_curte_post",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    guid_post = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    criado_em = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ultima_mudanca = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    esta_ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_curte_post", x => new { x.guid_post, x.id_usuario });
                    table.ForeignKey(
                        name: "FK_usuario_curte_post_post_guid_post",
                        column: x => x.guid_post,
                        principalTable: "post",
                        principalColumn: "guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_curte_post_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_curte_post_id_usuario",
                table: "usuario_curte_post",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario_curte_post");

            migrationBuilder.AlterColumn<int>(
                name: "categoria",
                table: "post",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
