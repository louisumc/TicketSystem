using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Tests.Helpers
{
    public static class TestData
    {
        public static Bus GetValidBus()
        {
            return new Bus
            {
                Id = Guid.NewGuid(),
                Plate = "ABC1234",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = 45,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Trip GetValidTrip()
        {
            var bus = GetValidBus();
            return new Trip
            {
                Id = Guid.NewGuid(),
                Origin = "Sao Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.UtcNow.AddHours(8),
                ArrivalTime = DateTime.UtcNow.AddHours(11),
                BusId = bus.Id,
                Bus = bus,
                Price = 120.00m,
                Status = TripStatus.Scheduled,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Seat GetValidSeat(Guid? tripId = null)
        {
            return new Seat
            {
                Id = Guid.NewGuid(),
                TripId = tripId ?? Guid.NewGuid(),
                Number = "1A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 1,
                Column = 1,
                PriceMultiplier = 1.10m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Passenger GetValidPassenger()
        {
            return new Passenger
            {
                Id = Guid.NewGuid(),
                Name = "Joao Silva",
                Document = "12345678901",
                Email = "joao@email.com",
                Phone = "11999999999",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Reservation GetValidReservation(Guid? tripId = null, Guid? passengerId = null)
        {
            return new Reservation
            {
                Id = Guid.NewGuid(),
                TripId = tripId ?? Guid.NewGuid(),
                PassengerId = passengerId ?? Guid.NewGuid(),
                ReservationDate = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                Status = ReservationStatus.Pending,
                TotalAmount = 120.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static List<Bus> GetBusList(int count = 3)
        {
            var buses = new List<Bus>();
            for (int i = 0; i < count; i++)
            {
                var plate = "ABC" + (i + 1).ToString() + "234";
                var model = "Modelo " + (i + 1).ToString();
                var company = "Empresa " + (i + 1).ToString();
                var capacity = 40 + i * 5;

                buses.Add(new Bus
                {
                    Id = Guid.NewGuid(),
                    Plate = plate,
                    Model = model,
                    Company = company,
                    Capacity = capacity,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return buses;
        }

        public static List<Trip> GetTripList(int count = 3)
        {
            var trips = new List<Trip>();
            var bus = GetValidBus();
            for (int i = 0; i < count; i++)
            {
                var origin = "Origem " + (i + 1).ToString();
                var destination = "Destino " + (i + 1).ToString();
                var price = 100 + (i * 50);

                trips.Add(new Trip
                {
                    Id = Guid.NewGuid(),
                    Origin = origin,
                    Destination = destination,
                    DepartureTime = DateTime.UtcNow.AddHours(i * 4),
                    ArrivalTime = DateTime.UtcNow.AddHours((i * 4) + 3),
                    BusId = bus.Id,
                    Bus = bus,
                    Price = price,
                    Status = TripStatus.Scheduled,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return trips;
        }

        public static List<Seat> GetSeatList(Guid tripId, int count = 5)
        {
            var seats = new List<Seat>();
            for (int i = 0; i < count; i++)
            {
                var row = (i / 4) + 1;
                var col = (i % 4) + 1;
                var letter = (char)('A' + col - 1);

                SeatType seatType;
                if (col == 1 || col == 4)
                    seatType = SeatType.Window;
                else if (col == 2)
                    seatType = SeatType.Aisle;
                else
                    seatType = SeatType.Middle;

                decimal priceMultiplier;
                if (col == 1 || col == 4)
                    priceMultiplier = 1.10m;
                else if (col == 2)
                    priceMultiplier = 1.05m;
                else
                    priceMultiplier = 1.00m;

                seats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    Number = row.ToString() + letter.ToString(),
                    Type = seatType,
                    Status = i < 3 ? SeatStatus.Available : SeatStatus.Reserved,
                    Row = row,
                    Column = col,
                    PriceMultiplier = priceMultiplier,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return seats;
        }
    }
}