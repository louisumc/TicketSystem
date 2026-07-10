namespace TicketSystem.Infrastructure.Cache
{
    public static class CacheKeys
    {
        public const string TripsListKey = "trips:list";
        public const string TripDetailsKey = "trip:details:{0}";
        public const string AvailableSeatsKey = "trip:seats:available:{0}";
        public const string ReservationKey = "reservation:{0}";
        public const string TripByBusKey = "trip:bus:{0}";
        public const string TripByStatusKey = "trip:status:{0}";
        public const string TripByDateRangeKey = "trip:date:{0}:{1}";

        public static string GetTripDetailsKey(Guid tripId) => string.Format(TripDetailsKey, tripId);
        public static string GetAvailableSeatsKey(Guid tripId) => string.Format(AvailableSeatsKey, tripId);
        public static string GetReservationKey(Guid reservationId) => string.Format(ReservationKey, reservationId);
        public static string GetTripByBusKey(Guid busId) => string.Format(TripByBusKey, busId);
        public static string GetTripByStatusKey(int status) => string.Format(TripByStatusKey, status);
        public static string GetTripByDateRangeKey(DateTime start, DateTime end) =>
            string.Format(TripByDateRangeKey, start.ToString("yyyyMMdd"), end.ToString("yyyyMMdd"));

        public static string[] AllTripPatterns => new[]
        {
            "trip:",
            "trips:",
            "trip:details:",        
            "trip:seats:available:",
            "trip:bus:",            
            "trip:status:",         
            "trip:date:"            
        };
    }
}