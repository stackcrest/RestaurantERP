using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactInquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactInquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HandledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactInquiries_AspNetUsers_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInquiries_CreatedAt",
                table: "ContactInquiries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInquiries_HandledByUserId",
                table: "ContactInquiries",
                column: "HandledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInquiries_Status",
                table: "ContactInquiries",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactInquiries");
        }
    }
}
