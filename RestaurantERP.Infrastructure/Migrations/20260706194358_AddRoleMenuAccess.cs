using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleMenuAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleMenuAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CanRead = table.Column<bool>(type: "bit", nullable: false),
                    CanWrite = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMenuAccesses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleMenuAccesses_RoleName_SectionKey",
                table: "RoleMenuAccesses",
                columns: new[] { "RoleName", "SectionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMenuAccesses");
        }
    }
}
