using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class CollegeUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status_curso",
                table: "usuario",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                name: "area_post",
                columns: table => new
                {
                    id_area = table.Column<int>(type: "int", nullable: false),
                    guid_post = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_area_post", x => new { x.id_area, x.guid_post });
                    table.ForeignKey(
                        name: "FK_area_post_area_id_area",
                        column: x => x.id_area,
                        principalTable: "area",
                        principalColumn: "id_area",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_area_post_post_guid_post",
                        column: x => x.guid_post,
                        principalTable: "post",
                        principalColumn: "guid",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_area_post_guid_post",
                table: "area_post",
                column: "guid_post");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "area_post");

            migrationBuilder.DropTable(
                name: "campus_oferece");

            migrationBuilder.DropTable(
                name: "area");

            migrationBuilder.DropColumn(
                name: "status_curso",
                table: "usuario");
        }
    }
}
