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
            // SEED DE ONIBUS
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

            // ============================================
            // SEED DE ASSENTOS
            // ============================================
            var trip1Id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var trip2Id = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");
            var trip3Id = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD");
            var trip4Id = Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE");

            // Assentos para viagem 1 (45 assentos)
            var seats1 = GenerateSeatsForTrip(trip1Id, 45);
            foreach (var seat in seats1)
            {
                migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "TripId", "Number", "Type", "Status", "Row", "Column", "PriceMultiplier", "IsActive", "CreatedAt" },
                values: seat
                );
            }

            // Assentos para viagem 2 (45 assentos)
            var seats2 = GenerateSeatsForTrip(trip2Id, 45);
            foreach (var seat in seats2)
            {
                migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "TripId", "Number", "Type", "Status", "Row", "Column", "PriceMultiplier", "IsActive", "CreatedAt" },
                values: seat
                );
            }

            // Assentos para viagem 3 (50 assentos)
            var seats3 = GenerateSeatsForTrip(trip3Id, 50);
            foreach (var seat in seats3)
            {
                migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "TripId", "Number", "Type", "Status", "Row", "Column", "PriceMultiplier", "IsActive", "CreatedAt" },
                values: seat
                );
            }

            // Assentos para viagem 4 (55 assentos)
            var seats4 = GenerateSeatsForTrip(trip4Id, 55);
            foreach (var seat in seats4)
            {
                migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "TripId", "Number", "Type", "Status", "Row", "Column", "PriceMultiplier", "IsActive", "CreatedAt" },
                values: seat
                );
            }
        }

        private static List<object[]> GenerateSeatsForTrip(Guid tripId, int capacity)
        {
            var seats = new List<object[]>();
            var columns = 4;
            var rows = (int)Math.Ceiling((double)capacity / columns);
            var seatCount = 0;

            for (int row = 1; row <= rows; row++)
            {
                for (int col = 1; col <= columns; col++)
                {
                    if (seatCount >= capacity)
                        break;

                    var columnLetter = (char)('A' + col - 1);
                    var number = $"{row}{columnLetter}";

                    int seatType;
                    if (col == 1 || col == columns)
                        seatType = 0; // Window
                    else if (col == 2)
                        seatType = 1; // Aisle
                    else
                        seatType = 2; // Middle

                    decimal priceMultiplier;
                    if (col == 1 || col == columns)
                        priceMultiplier = 1.10m;
                    else if (col == 2)
                        priceMultiplier = 1.05m;
                    else
                        priceMultiplier = 1.00m;

                    int status = 0; // Available

                    if (row == 1 && col == 1)
                        status = 1; // Reserved - 1A
                    else if (row == 1 && col == 4)
                        status = 2; // Sold - 1D
                    else if (row == 2 && col == 2)
                        status = 1; // Reserved - 2B

                    seats.Add(new object[]
                    {
Guid.NewGuid(),
tripId,
number,
seatType,
status,
row,
col,
priceMultiplier,
true,
Now
                    });

                    seatCount++;
                }
            }

            return seats;
        }

        public static void Down(MigrationBuilder migrationBuilder)
        {
            // Remove todos os assentos
            migrationBuilder.Sql("DELETE FROM Seats");

            // Remove as viagens
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

            // Remove os ônibus
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