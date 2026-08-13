using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Genius.PriceChecker.Db.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    PricePattern = table.Column<string>(type: "TEXT", nullable: false),
                    Handler = table.Column<string>(type: "TEXT", nullable: false),
                    DecimalDelimiter = table.Column<char>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentArgument = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSources_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSources_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: true),
                    FoundDate = table.Column<long>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPrices_ProductSources_ProductSourceId",
                        column: x => x.ProductSourceId,
                        principalTable: "ProductSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Key",
                table: "Agents",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductSourceId",
                table: "ProductPrices",
                column: "ProductSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSources_AgentId",
                table: "ProductSources",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSources_ProductId",
                table: "ProductSources",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPrices");

            migrationBuilder.DropTable(
                name: "ProductSources");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
