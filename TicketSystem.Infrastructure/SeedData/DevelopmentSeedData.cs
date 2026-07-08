using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.SeedData
{
    public static class DevelopmentSeedData
    {
        private static readonly DateTime Now = DateTime.UtcNow;

        public static void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // SEED DE ÔNIBUS
            // ============================================
            migrationBuilder.InsertData(
            table: "Buses",
            columns: new[] { "Id", "Plate", "Model", "Company", "Capacity", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{
Guid.Parse("11111111-1111-1111-1111-111111111111"),
"ABC1234",
"Mercedes Benz O500",
"Viação Expresso",
45,
true,
Now
},
{
Guid.Parse("22222222-2222-2222-2222-222222222222"),
"DEF5678",
"Scania K400",
"Viação Rápida",
50,
true,
Now
},
{
Guid.Parse("33333333-3333-3333-3333-333333333333"),
"GHI9012",
"Volvo 9800",
"Viação Conforto",
55,
true,
Now
},
{
Guid.Parse("44444444-4444-4444-4444-444444444444"),
"JKL3456",
"Marcopolo Paradiso",
"Viação Turismo",
48,
true,
Now
},
{
Guid.Parse("55555555-5555-5555-5555-555555555555"),
"MNO7890",
"Volkswagen Volksbus",
"Viação Capital",
40,
false,
Now
}
            });

            // ============================================
            // SEED DE VIAGENS
            // ============================================
            migrationBuilder.InsertData(
            table: "Trips",
            columns: new[] { "Id", "Origin", "Destination", "DepartureTime", "ArrivalTime", "BusId", "Price", "Status", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{
Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
"São Paulo",
"Rio de Janeiro",
Now.AddDays(1).AddHours(8),
Now.AddDays(1).AddHours(11),
Guid.Parse("11111111-1111-1111-1111-111111111111"),
120.00m,
0,
true,
Now
},
{
Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
"São Paulo",
"Curitiba",
Now.AddDays(2).AddHours(10),
Now.AddDays(2).AddHours(14),
Guid.Parse("11111111-1111-1111-1111-111111111111"),
80.00m,
0,
true,
Now
},
{
Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD"),
"Rio de Janeiro",
"Belo Horizonte",
Now.AddDays(1).AddHours(9),
Now.AddDays(1).AddHours(13),
Guid.Parse("22222222-2222-2222-2222-222222222222"),
100.00m,
0,
true,
Now
},
{
Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE"),
"Curitiba",
"Florianópolis",
Now.AddDays(3).AddHours(7),
Now.AddDays(3).AddHours(11),
Guid.Parse("33333333-3333-3333-3333-333333333333"),
70.00m,
0,
true,
Now
}
            });
        }

        public static void Down(MigrationBuilder migrationBuilder)
        {
            // Remove os dados inseridos
            migrationBuilder.DeleteData(
            table: "Trips",
            keyColumn: "Id",
            keyValues: new object[]
            {
Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD"),
Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE")
            });

            migrationBuilder.DeleteData(
            table: "Buses",
            keyColumn: "Id",
            keyValues: new object[]
            {
Guid.Parse("11111111-1111-1111-1111-111111111111"),
Guid.Parse("22222222-2222-2222-2222-222222222222"),
Guid.Parse("33333333-3333-3333-3333-333333333333"),
Guid.Parse("44444444-4444-4444-4444-444444444444"),
Guid.Parse("55555555-5555-5555-5555-555555555555")
            });
        }
    }
}