using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelScheduleArrange.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    SourceId = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attractions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Itineraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetLocation = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLatitude = table.Column<double>(type: "REAL", nullable: true),
                    TargetLongitude = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itineraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItineraryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItineraryId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttractionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromPrevious = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryItems_Attractions_AttractionId",
                        column: x => x.AttractionId,
                        principalTable: "Attractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItineraryItems_Itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "Itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attractions_SourceId",
                table: "Attractions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryItems_AttractionId",
                table: "ItineraryItems",
                column: "AttractionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryItems_ItineraryId_Order",
                table: "ItineraryItems",
                columns: new[] { "ItineraryId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItineraryItems");

            migrationBuilder.DropTable(
                name: "Attractions");

            migrationBuilder.DropTable(
                name: "Itineraries");
        }
    }
}
