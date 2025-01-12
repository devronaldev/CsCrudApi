using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CsCrudApi.Migrations
{
    /// <inheritdoc />
    public partial class ExternalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "link_externo",
                table: "post",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "link_externo",
                table: "post");
        }
    }
}
