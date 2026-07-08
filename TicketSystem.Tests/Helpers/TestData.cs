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
                Origin = "São Paulo",
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

        public static List<Bus> GetBusList(int count = 3)
        {
            var buses = new List<Bus>();
            for (int i = 0; i < count; i++)
            {
                buses.Add(new Bus
                {
                    Id = Guid.NewGuid(),
                    Plate = $"ABC{i + 1}234",
                    Model = $"Modelo {i + 1}",
                    Company = $"Empresa {i + 1}",
                    Capacity = 40 + i * 5,
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
                trips.Add(new Trip
                {
                    Id = Guid.NewGuid(),
                    Origin = $"Origem {i + 1}",
                    Destination = $"Destino {i + 1}",
                    DepartureTime = DateTime.UtcNow.AddHours(i * 4),
                    ArrivalTime = DateTime.UtcNow.AddHours((i * 4) + 3),
                    BusId = bus.Id,
                    Bus = bus,
                    Price = 100 + (i * 50),
                    Status = TripStatus.Scheduled,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return trips;
        }
    }
}
