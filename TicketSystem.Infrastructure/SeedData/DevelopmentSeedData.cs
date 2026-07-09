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
            // 1. GERAR E GUARDAR TODOS OS GUIDS
            // ============================================
            var bus1Id = Guid.NewGuid();
            var bus2Id = Guid.NewGuid();
            var bus3Id = Guid.NewGuid();
            var bus4Id = Guid.NewGuid();
            var bus5Id = Guid.NewGuid();

            var trip1Id = Guid.NewGuid();
            var trip2Id = Guid.NewGuid();
            var trip3Id = Guid.NewGuid();
            var trip4Id = Guid.NewGuid();

            // Assentos com GUIDs fixos para referência
            var seat1A = Guid.NewGuid();
            var seat1B = Guid.NewGuid();
            var seat1C = Guid.NewGuid();
            var seat1D = Guid.NewGuid();
            var seat2A = Guid.NewGuid();
            var seat2B = Guid.NewGuid();

            var seatTrip2_1A = Guid.NewGuid();
            var seatTrip2_1B = Guid.NewGuid();
            var seatTrip2_2A = Guid.NewGuid();
            var seatTrip2_2B = Guid.NewGuid();

            var passenger1Id = Guid.NewGuid();
            var passenger2Id = Guid.NewGuid();
            var passenger3Id = Guid.NewGuid();

            var reservation1Id = Guid.NewGuid();
            var reservation2Id = Guid.NewGuid();

            // ============================================
            // 2. INSERIR ONIBUS
            // ============================================
            migrationBuilder.InsertData(
            table: "Buses",
            columns: new[] { "Id", "Plate", "Model", "Company", "Capacity", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ bus1Id, "ABC1234", "Mercedes Benz O500", "Viação Expresso", 45, true, Now },
{ bus2Id, "DEF5678", "Scania K400", "Viação Rápida", 50, true, Now },
{ bus3Id, "GHI9012", "Volvo 9800", "Viação Conforto", 55, true, Now },
{ bus4Id, "JKL3456", "Marcopolo Paradiso", "Viação Turismo", 48, true, Now },
{ bus5Id, "MNO7890", "Volkswagen Volksbus", "Viação Capital", 40, false, Now }
            });

            // ============================================
            // 3. INSERIR VIAGENS
            // ============================================
            migrationBuilder.InsertData(
            table: "Trips",
            columns: new[] { "Id", "Origin", "Destination", "DepartureTime", "ArrivalTime", "BusId", "Price", "Status", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ trip1Id, "Sao Paulo", "Rio de Janeiro", Now.AddDays(1).AddHours(8), Now.AddDays(1).AddHours(11), bus1Id, 120.00m, 0, true, Now },
{ trip2Id, "Sao Paulo", "Curitiba", Now.AddDays(2).AddHours(10), Now.AddDays(2).AddHours(14), bus1Id, 80.00m, 0, true, Now },
{ trip3Id, "Rio de Janeiro", "Belo Horizonte", Now.AddDays(1).AddHours(9), Now.AddDays(1).AddHours(13), bus2Id, 100.00m, 0, true, Now },
{ trip4Id, "Curitiba", "Florianopolis", Now.AddDays(3).AddHours(7), Now.AddDays(3).AddHours(11), bus3Id, 70.00m, 0, true, Now }
            });

            // ============================================
            // 4. INSERIR ASSENTOS (usando os GUIDs fixos)
            // ============================================
            // Viagem 1 - 6 assentos com GUIDs fixos
            migrationBuilder.InsertData(
            table: "Seats",
            columns: new[] { "Id", "TripId", "Number", "Type", "Status", "Row", "Column", "PriceMultiplier", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ seat1A, trip1Id, "1A", 0, 0, 1, 1, 1.10m, true, Now },
{ seat1B, trip1Id, "1B", 1, 0, 1, 2, 1.05m, true, Now },
{ seat1C, trip1Id, "1C", 2, 0, 1, 3, 1.00m, true, Now },
{ seat1D, trip1Id, "1D", 0, 0, 1, 4, 1.10m, true, Now },
{ seat2A, trip1Id, "2A", 0, 0, 2, 1, 1.10m, true, Now },
{ seat2B, trip1Id, "2B", 1, 0, 2, 2, 1.05m, true, Now }
            });

            // Viagem 2 - 4 assentos com GUIDs fixos
            migrationBuilder.InsertData(
            table: "Seats",
            columns: new[] { "Id", "TripId", "Number", "Type", "Status", "Row", "Column", "PriceMultiplier", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ seatTrip2_1A, trip2Id, "1A", 0, 0, 1, 1, 1.10m, true, Now },
{ seatTrip2_1B, trip2Id, "1B", 1, 0, 1, 2, 1.05m, true, Now },
{ seatTrip2_2A, trip2Id, "2A", 0, 0, 2, 1, 1.10m, true, Now },
{ seatTrip2_2B, trip2Id, "2B", 1, 0, 2, 2, 1.05m, true, Now }
            });

            // ============================================
            // 5. INSERIR PASSAGEIROS
            // ============================================
            migrationBuilder.InsertData(
            table: "Passengers",
            columns: new[] { "Id", "Name", "Document", "Email", "Phone", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ passenger1Id, "Joao Silva", "12345678901", "joao.silva@email.com", "11999999999", true, Now },
{ passenger2Id, "Maria Santos", "98765432100", "maria.santos@email.com", "11888888888", true, Now },
{ passenger3Id, "Pedro Oliveira", "45678912300", "pedro.oliveira@email.com", "11777777777", true, Now }
            });

            // ============================================
            // 6. INSERIR RESERVAS
            // ============================================
            migrationBuilder.InsertData(
            table: "Reservations",
            columns: new[] { "Id", "TripId", "PassengerId", "ReservationDate", "ExpiresAt", "Status", "TotalAmount", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ reservation1Id, trip1Id, passenger1Id, Now.AddMinutes(-5), Now.AddMinutes(10), 0, 132.00m, true, Now.AddMinutes(-5) },
{ reservation2Id, trip2Id, passenger2Id, Now.AddMinutes(-2), Now.AddMinutes(13), 0, 88.00m, true, Now.AddMinutes(-2) }
            });

            // ============================================
            // 7. INSERIR RESERVATION SEATS (COM REFERENCIAS)
            // ============================================
            migrationBuilder.InsertData(
            table: "ReservationSeats",
            columns: new[] { "Id", "ReservationId", "SeatId", "Price", "IsActive", "CreatedAt" },
            values: new object[,]
            {
{ Guid.NewGuid(), reservation1Id, seat1A, 132.00m, true, Now.AddMinutes(-5) },
{ Guid.NewGuid(), reservation1Id, seat1B, 126.00m, true, Now.AddMinutes(-5) },
{ Guid.NewGuid(), reservation2Id, seatTrip2_2A, 88.00m, true, Now.AddMinutes(-2) }
            });
        }

        public static void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM ReservationSeats");
            migrationBuilder.Sql("DELETE FROM Reservations");
            migrationBuilder.Sql("DELETE FROM Passengers");
            migrationBuilder.Sql("DELETE FROM Seats");
            migrationBuilder.Sql("DELETE FROM Trips");
            migrationBuilder.Sql("DELETE FROM Buses");
        }
    }
}