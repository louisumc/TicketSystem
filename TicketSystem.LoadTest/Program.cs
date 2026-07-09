using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TicketSystem.SimpleTest;

class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private static readonly Random _random = new Random();

    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine(" TESTE SIMPLES DE RESERVA");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var baseUrl = "http://localhost:5000";
        _httpClient.BaseAddress = new Uri(baseUrl);

        // 1. Verificar se a API esta rodando
        Console.WriteLine("1. Verificando API...");
        try
        {
            var testResponse = await _httpClient.GetAsync("/api/buses");
            if (!testResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("ERRO: API nao esta rodando!");
                Console.WriteLine("Execute: cd TicketSystem.Api && dotnet run --urls=http://localhost:5000");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("   API esta rodando!");
        }
        catch
        {
            Console.WriteLine("ERRO: API nao esta acessivel!");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();

        // 2. Buscar todas as viagens com assentos disponiveis
        Console.WriteLine("2. Buscando viagens com assentos disponiveis...");
        var availableTrips = await GetAvailableTrips();

        if (availableTrips == null || availableTrips.Count == 0)
        {
            Console.WriteLine("ERRO: Nenhuma viagem com assentos disponiveis encontrada!");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"   Total de viagens com assentos disponiveis: {availableTrips.Count}");

        // Mostrar todas as viagens disponiveis
        Console.WriteLine("   Viagens disponiveis:");
        for (int i = 0; i < availableTrips.Count; i++)
        {
            var t = availableTrips[i];
            Console.WriteLine($"   {i + 1}. {t.Origin} -> {t.Destination} (Assentos disponiveis: {t.AvailableSeats})");
        }
        Console.WriteLine();

        // 3. Selecionar uma viagem aleatoria
        var randomIndex = _random.Next(0, availableTrips.Count);
        var trip = availableTrips[randomIndex];

        Console.WriteLine($"3. Viagem selecionada (aleatoria #{randomIndex + 1}):");
        Console.WriteLine($"   Origem: {trip.Origin}");
        Console.WriteLine($"   Destino: {trip.Destination}");
        Console.WriteLine($"   ID: {trip.Id}");
        Console.WriteLine($"   Assentos disponiveis: {trip.AvailableSeats}");
        Console.WriteLine();

        // 4. Escolher um assento aleatorio entre os disponiveis
        var seatIndex = _random.Next(0, trip.SeatNumbers.Count);
        var seatNumber = trip.SeatNumbers[seatIndex];
        Console.WriteLine($"4. Assento escolhido (aleatorio): {seatNumber}");
        Console.WriteLine($"   Assentos disponiveis: {string.Join(", ", trip.SeatNumbers)}");
        Console.WriteLine();

        // 5. Fazer a reserva
        Console.WriteLine("5. Criando reserva...");
        var reservation = await CreateReservation(trip.Id, seatNumber);

        if (reservation == null)
        {
            Console.WriteLine("   ERRO: Falha ao criar reserva!");
            Console.ReadKey();
            return;
        }

        string statusTexto = reservation.Status == "0" ? "Pendente" :
                             reservation.Status == "1" ? "Confirmada" :
                             reservation.Status == "2" ? "Cancelada" :
                             reservation.Status == "3" ? "Expirada" : "Desconhecido";

        Console.WriteLine($"   Reserva criada com sucesso!");
        Console.WriteLine($"   ID da reserva: {reservation.Id}");
        Console.WriteLine($"   Status: {statusTexto} (codigo: {reservation.Status})");
        Console.WriteLine();

        // 6. Verificar se a reserva foi criada corretamente
        Console.WriteLine("6. Verificando reserva criada...");
        var verifiedReservation = await GetReservation(reservation.Id);

        if (verifiedReservation != null)
        {
            string statusVerificado = verifiedReservation.Status == "0" ? "Pendente" :
                                       verifiedReservation.Status == "1" ? "Confirmada" :
                                       verifiedReservation.Status == "2" ? "Cancelada" :
                                       verifiedReservation.Status == "3" ? "Expirada" : "Desconhecido";

            Console.WriteLine($"   Reserva confirmada!");
            Console.WriteLine($"   ID: {verifiedReservation.Id}");
            Console.WriteLine($"   Status: {statusVerificado} (codigo: {verifiedReservation.Status})");
            Console.WriteLine($"   Viagem: {verifiedReservation.TripOrigin} -> {verifiedReservation.TripDestination}");
            Console.WriteLine($"   Data/Hora: {verifiedReservation.TripDepartureTime}");

            // Mostrar os assentos
            if (verifiedReservation.Seats != null && verifiedReservation.Seats.Count > 0)
            {
                var seatNumbers = verifiedReservation.Seats.Select(s => s.SeatNumber).ToList();
                Console.WriteLine($"   Assentos: {string.Join(", ", seatNumbers)}");

                // Mostrar detalhes de cada assento
                foreach (var seat in verifiedReservation.Seats)
                {
                    Console.WriteLine($"      - Assento {seat.SeatNumber} (Fileira: {seat.Row}, Coluna: {seat.Column}, Preco: R$ {seat.Price:F2})");
                }
            }
            else
            {
                Console.WriteLine($"   Assentos: Nenhum assento encontrado");
            }

            Console.WriteLine($"   Passageiro: {verifiedReservation.PassengerName}");
            Console.WriteLine($"   Documento: {verifiedReservation.PassengerDocument}");
            Console.WriteLine($"   Email: {verifiedReservation.PassengerEmail}");
            Console.WriteLine($"   Telefone: {verifiedReservation.PassengerPhone}");
            Console.WriteLine($"   Valor total: R$ {verifiedReservation.TotalAmount:F2}");
            Console.WriteLine($"   Expira em: {verifiedReservation.ExpiresAt}");
        }
        else
        {
            Console.WriteLine("   Nao foi possivel verificar a reserva");
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" TESTE CONCLUIDO COM SUCESSO!");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }

    static async Task<List<TripWithSeats>> GetAvailableTrips()
    {
        try
        {
            // Buscar todas as viagens
            var response = await _httpClient.GetAsync("/api/trips");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"   Erro ao buscar viagens: {response.StatusCode}");
                return new List<TripWithSeats>();
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TripResponse>>>();
            if (result?.Data == null || result.Data.Count == 0)
            {
                Console.WriteLine("   Nenhuma viagem encontrada!");
                return new List<TripWithSeats>();
            }

            Console.WriteLine($"   Total de viagens encontradas: {result.Data.Count}");

            var tripsWithSeats = new List<TripWithSeats>();

            // Para cada viagem, buscar os assentos
            foreach (var trip in result.Data)
            {
                var seatsResponse = await _httpClient.GetAsync($"/api/seats/trip/{trip.Id}");
                if (!seatsResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   Erro ao buscar assentos da viagem {trip.Id}: {seatsResponse.StatusCode}");
                    continue;
                }

                var responseBody = await seatsResponse.Content.ReadAsStringAsync();

                try
                {
                    var seatsResult = JsonSerializer.Deserialize<ApiResponse<List<SeatResponse>>>(responseBody, _jsonOptions);

                    if (seatsResult?.Data != null)
                    {
                        var availableSeats = seatsResult.Data
                            .Where(s => s.Status == 0)
                            .Select(s => s.Number)
                            .ToList();

                        if (availableSeats.Count > 0)
                        {
                            tripsWithSeats.Add(new TripWithSeats
                            {
                                Id = trip.Id,
                                Origin = trip.Origin,
                                Destination = trip.Destination,
                                SeatNumbers = availableSeats,
                                AvailableSeats = availableSeats.Count
                            });

                            Console.WriteLine($"   Viagem {trip.Origin} -> {trip.Destination}: {availableSeats.Count} assentos disponiveis");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"   Erro ao processar assentos da viagem {trip.Id}: {ex.Message}");
                }
            }

            return tripsWithSeats;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar viagens: {ex.Message}");
            return new List<TripWithSeats>();
        }
    }

    static async Task<ReservationResponse?> CreateReservation(string tripId, string seatNumber)
    {
        try
        {
            var cpf = _random.Next(100000000, 999999999).ToString("000000000") +
                      _random.Next(10, 99).ToString("00");

            var request = new
            {
                TripId = tripId,
                Passenger = new
                {
                    Name = "Joao Silva",
                    Document = cpf,
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new[] { seatNumber }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/reservations", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"   Erro na criacao: {response.StatusCode}");
                Console.WriteLine($"   Detalhe: {responseBody}");
                return null;
            }

            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<ReservationResponse>>(responseBody, _jsonOptions);
                return result?.Data;
            }
            catch (JsonException)
            {
                var alternativeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    Converters = { new StatusIntToStringConverter() }
                };

                var result = JsonSerializer.Deserialize<ApiResponse<ReservationResponseAlternative>>(responseBody, alternativeOptions);
                if (result?.Data != null)
                {
                    return new ReservationResponse
                    {
                        Id = result.Data.Id,
                        Status = result.Data.Status.ToString()
                    };
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar reserva: {ex.Message}");
            return null;
        }
    }

    static async Task<ReservationDetailResponse?> GetReservation(Guid reservationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/reservations/{reservationId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var responseBody = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<ReservationDetailResponse>>(responseBody, _jsonOptions);
                return result?.Data;
            }
            catch (JsonException)
            {
                var alternativeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    Converters = { new StatusIntToStringConverter() }
                };

                var result = JsonSerializer.Deserialize<ApiResponse<ReservationDetailResponseAlternative>>(responseBody, alternativeOptions);
                if (result?.Data != null)
                {
                    return new ReservationDetailResponse
                    {
                        Id = result.Data.Id,
                        Status = result.Data.Status.ToString(),
                        TripId = result.Data.TripId ?? string.Empty,
                        TripOrigin = result.Data.TripOrigin ?? string.Empty,
                        TripDestination = result.Data.TripDestination ?? string.Empty,
                        TripDepartureTime = result.Data.TripDepartureTime,
                        PassengerName = result.Data.PassengerName ?? string.Empty,
                        PassengerDocument = result.Data.PassengerDocument ?? string.Empty,
                        PassengerEmail = result.Data.PassengerEmail ?? string.Empty,
                        PassengerPhone = result.Data.PassengerPhone ?? string.Empty,
                        TotalAmount = result.Data.TotalAmount,
                        ExpiresAt = result.Data.ExpiresAt,
                        Seats = result.Data.Seats?.Select(s => new SeatDetail
                        {
                            Id = s.Id ?? string.Empty,
                            SeatId = s.SeatId ?? string.Empty,
                            SeatNumber = s.SeatNumber ?? string.Empty,
                            SeatType = s.SeatType,
                            Row = s.Row,
                            Column = s.Column,
                            Price = s.Price
                        }).ToList() ?? new List<SeatDetail>()
                    };
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao verificar reserva: {ex.Message}");
            return null;
        }
    }

    // Models
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class TripResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }

    public class SeatResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TripId { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Status { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public string? PassengerName { get; set; }
        public string? PassengerDocument { get; set; }
        public double PriceMultiplier { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class TripWithSeats
    {
        public string Id { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public List<string> SeatNumbers { get; set; } = new List<string>();
        public int AvailableSeats { get; set; }
    }

    public class ReservationResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ReservationResponseAlternative
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
    }

    // Model completo da reserva
    public class ReservationDetailResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TripId { get; set; } = string.Empty;
        public string TripOrigin { get; set; } = string.Empty;
        public string TripDestination { get; set; } = string.Empty;
        public DateTime TripDepartureTime { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerDocument { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<SeatDetail> Seats { get; set; } = new List<SeatDetail>();
    }

    public class SeatDetail
    {
        public string Id { get; set; } = string.Empty;
        public string SeatId { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public int SeatType { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public decimal Price { get; set; }
    }

    public class ReservationDetailResponseAlternative
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string TripId { get; set; } = string.Empty;
        public string TripOrigin { get; set; } = string.Empty;
        public string TripDestination { get; set; } = string.Empty;
        public DateTime TripDepartureTime { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerDocument { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<SeatDetailAlternative> Seats { get; set; } = new List<SeatDetailAlternative>();
    }

    public class SeatDetailAlternative
    {
        public string Id { get; set; } = string.Empty;
        public string SeatId { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public int SeatType { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public decimal Price { get; set; }
    }

    public class StatusIntToStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                var intValue = reader.GetInt32();
                return intValue.ToString();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}