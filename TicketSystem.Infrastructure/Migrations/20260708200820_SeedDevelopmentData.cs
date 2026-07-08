using Microsoft.EntityFrameworkCore.Migrations;
using TicketSystem.Infrastructure.SeedData;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    public partial class SeedDevelopmentData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DevelopmentSeedData.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DevelopmentSeedData.Down(migrationBuilder);
        }
    }
}