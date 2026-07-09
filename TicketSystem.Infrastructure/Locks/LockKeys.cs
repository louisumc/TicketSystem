namespace TicketSystem.Infrastructure.Locks
{
    public static class LockKeys
    {
        public const string TripReservation = "trip:reservation:{0}";
        public const string SeatUpdate = "seat:update:{0}";
        public const string ReservationConfirm = "reservation:confirm:{0}";

        public static string GetTripReservationKey(Guid tripId) => string.Format(TripReservation, tripId);
        public static string GetSeatUpdateKey(Guid seatId) => string.Format(SeatUpdate, seatId);
        public static string GetReservationConfirmKey(Guid reservationId) => string.Format(ReservationConfirm, reservationId);
    }
}