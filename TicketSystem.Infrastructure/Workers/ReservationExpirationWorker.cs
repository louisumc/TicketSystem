using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TicketSystem.Application.Events;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Workers
{
    public class ReservationExpirationWorker : BackgroundService
    {
        private readonly ILogger<ReservationExpirationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval;
        private readonly bool _isEnabled;
        private readonly IModel _channel;

        public ReservationExpirationWorker(
        ILogger<ReservationExpirationWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IConnection connection)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _interval = TimeSpan.FromMinutes(configuration.GetValue<int>("Workers:ReservationExpirationIntervalMinutes", 1));
            _isEnabled = configuration.GetValue<bool>("Workers:ReservationExpirationEnabled", true);
            _channel = connection.CreateModel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("ReservationExpirationWorker desabilitado");
                return;
            }

            _logger.LogInformation("ReservationExpirationWorker iniciado. Intervalo: {Interval} minutos", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredReservations(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar reservas expiradas");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("ReservationExpirationWorker finalizado");
        }

        private async Task ProcessExpiredReservations(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            _logger.LogDebug("Verificando reservas expiradas...");

            var expiredReservations = await reservationService.GetExpiredReservationsAsync(cancellationToken);

            if (!expiredReservations.Any())
            {
                _logger.LogDebug("Nenhuma reserva expirada encontrada");
                return;
            }

            var count = expiredReservations.Count();
            _logger.LogInformation("Encontradas {Count} reservas expiradas", count);

            foreach (var reservation in expiredReservations)
            {
                try
                {
                    await reservationService.CancelReservationAsync(reservation.Id);

                    var expiredEvent = new ReservationExpiredEvent
                    {
                        ReservationId = reservation.Id,
                        TripId = reservation.TripId,
                        PassengerId = reservation.PassengerId,
                        PassengerName = reservation.PassengerName,
                        PassengerEmail = reservation.PassengerEmail,
                        SeatNumbers = reservation.Seats.Select(s => s.SeatNumber).ToList(),
                        ExpiredAt = DateTime.UtcNow
                    };

                    await eventPublisher.PublishAsync(expiredEvent, cancellationToken);

                    _logger.LogInformation("Reserva expirada processada: {ReservationId}", reservation.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar reserva expirada: {ReservationId}", reservation.Id);
                }
            }
        }

        public override void Dispose()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                    _channel.Dispose();
                }
                catch { }
            }
            base.Dispose();
        }
    }
}