using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "area",
                columns: table => new
                {
                    id_area = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    desc_area = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_area", x => x.id_area);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "campus",
                columns: table => new
                {
                    id_campus = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sg_campus = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    desc_campus = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    end_campus = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email_campus = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cd_cidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campus", x => x.id_campus);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "campus_oferece",
                columns: table => new
                {
                    IdCourse = table.Column<int>(type: "int", nullable: false),
                    IdCampus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campus_oferece", x => new { x.IdCampus, x.IdCourse });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome_categoria = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cidade",
                columns: table => new
                {
                    id_cidade = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado = table.Column<int>(type: "int", nullable: false),
                    cod_ibge = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cidade", x => x.id_cidade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "emailverification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    novo_email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    verification_token = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_verified = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emailverification", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cd_campus = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    senha = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_nascimento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    tipo_interesse = table.Column<int>(type: "int", nullable: false),
                    grau_escolaridade = table.Column<int>(type: "int", nullable: false),
                    nome_social = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo_cor = table.Column<int>(type: "int", nullable: false),
                    cd_cidade = table.Column<int>(type: "int", nullable: false),
                    is_email_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status_curso = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id_usuario);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "post",
                columns: table => new
                {
                    guid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_post = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    desc_post = table.Column<string>(type: "longtext", maxLength: 16000000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    qt_like = table.Column<int>(type: "int", nullable: false),
                    tipo_post = table.Column<int>(type: "int", nullable: false),
                    descricao_titulo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_usuario = table.Column<int>(type: "int", nullable: false),
                    area = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post", x => x.guid);
                    table.ForeignKey(
                        name: "FK_post_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario_seguindo_usuario",
                columns: table => new
                {
                    cd_usuario_seguidor = table.Column<int>(type: "int", nullable: false),
                    cd_usuario_seguido = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_seguindo_usuario", x => new { x.cd_usuario_seguidor, x.cd_usuario_seguido });
                    table.ForeignKey(
                        name: "FK_usuario_seguindo_usuario_usuario_cd_usuario_seguido",
                        column: x => x.cd_usuario_seguido,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuario_seguindo_usuario_usuario_cd_usuario_seguidor",
                        column: x => x.cd_usuario_seguidor,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "post_tem_categoria",
                columns: table => new
                {
                    guid_post = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_categoria = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_tem_categoria", x => new { x.guid_post, x.id_categoria });
                    table.ForeignKey(
                        name: "FK_post_tem_categoria_categorias_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_tem_categoria_post_guid_post",
                        column: x => x.guid_post,
                        principalTable: "post",
                        principalColumn: "guid",
                        onDelete: ReferentialAction.Cascade);
                })
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
                name: "IX_post_id_usuario",
                table: "post",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_post_tem_categoria_id_categoria",
                table: "post_tem_categoria",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_curte_post_id_usuario",
                table: "usuario_curte_post",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_seguindo_usuario_cd_usuario_seguido",
                table: "usuario_seguindo_usuario",
                column: "cd_usuario_seguido");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "area");

            migrationBuilder.DropTable(
                name: "campus");

            migrationBuilder.DropTable(
                name: "campus_oferece");

            migrationBuilder.DropTable(
                name: "cidade");

            migrationBuilder.DropTable(
                name: "emailverification");

            migrationBuilder.DropTable(
                name: "post_tem_categoria");

            migrationBuilder.DropTable(
                name: "usuario_curte_post");

            migrationBuilder.DropTable(
                name: "usuario_seguindo_usuario");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "post");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
