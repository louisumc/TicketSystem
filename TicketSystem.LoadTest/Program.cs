using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TicketSystem.LoadTest;

class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private static readonly Random _random = new Random();
    private static readonly int MAX_RETRY_ATTEMPTS = 30;
    private static readonly int RETRY_DELAY_SECONDS = 2;

    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("   TESTE DE CONCORRENCIA - RESERVAS");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.WriteLine("ATENCAO: Durante o teste, pausas serao feitas para:");
        Console.WriteLine("   - Verificar filas no RabbitMQ (http://localhost:15672)");
        Console.WriteLine("   - Verificar cache no Redis (redis-cli)");
        Console.WriteLine("   - Verificar dados no SQL Server");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();

        var baseUrl = "http://localhost:5000";
        _httpClient.BaseAddress = new Uri(baseUrl);

        Console.WriteLine("Aguardando API iniciar...");

        var apiReady = await WaitForApiReady(baseUrl);
        if (!apiReady)
        {
            Console.WriteLine();
            Console.WriteLine("ERRO: API nao iniciou apos " + (MAX_RETRY_ATTEMPTS * RETRY_DELAY_SECONDS) + " segundos!");
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("API esta rodando!");
        Console.WriteLine();

        await CheckServicesStatus();

        Console.WriteLine();
        Console.WriteLine("Buscando viagens com assentos disponiveis...");

        var trips = await GetTripsWithAvailableSeats();

        if (trips == null || trips.Count == 0)
        {
            Console.WriteLine("ERRO: Nenhuma viagem com assentos disponiveis encontrada!");
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        var validTrips = trips.Where(t => t.AvailableSeats >= 4).ToList();
        if (validTrips.Count == 0)
        {
            Console.WriteLine("ERRO: Nenhuma viagem com pelo menos 4 assentos disponiveis!");
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        var randomIndex = _random.Next(0, validTrips.Count);
        var trip = validTrips[randomIndex];
        var availableSeats = trip.SeatNumbers.Take(4).ToList();

        if (availableSeats.Count < 4)
        {
            Console.WriteLine("ERRO: Poucos assentos disponiveis!");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Viagem encontrada (aleatoria): " + trip.Id);
        Console.WriteLine("   Origem: " + trip.Origin);
        Console.WriteLine("   Destino: " + trip.Destination);
        Console.WriteLine("   Assentos disponiveis: " + trip.AvailableSeats);
        Console.WriteLine("   Assentos para teste: " + string.Join(", ", availableSeats));

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas no RabbitMQ antes de comecar:");
        Console.WriteLine("   http://localhost:15672");
        Console.WriteLine("   Usuario: guest / Senha: guest");
        Console.WriteLine("   Filas: reservation.created, reservation.confirmed, payment.failed, ticket.generated");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER quando estiver pronto para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   TESTE 1: MESMO ASSENTO");
        Console.WriteLine("   10 usuarios tentando reservar o assento " + availableSeats[0]);
        Console.WriteLine("========================================");
        Console.WriteLine();

        await TestSameSeat(trip.Id, availableSeats[0]);

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas do RabbitMQ apos o Teste 1:");
        Console.WriteLine("   Fila: reservation.created - deve ter 1 mensagem");
        Console.WriteLine("   Fila: reservation.confirmed - deve estar vazia (consumidor processou)");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("Atualizando lista de assentos disponiveis apos Teste 1...");
        var tripsAfterTest1 = await GetTripsWithAvailableSeats();
        var tripAfterTest1 = tripsAfterTest1.FirstOrDefault(t => t.Id == trip.Id);

        if (tripAfterTest1 == null || tripAfterTest1.AvailableSeats < 3)
        {
            Console.WriteLine("ERRO: Poucos assentos disponiveis para continuar!");
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        var seatsForTest2 = tripAfterTest1.SeatNumbers.Take(4).ToList();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   TESTE 2: ASSENTOS DIFERENTES");
        Console.WriteLine("   4 usuarios diferentes reservando 4 assentos diferentes");
        Console.WriteLine("========================================");
        Console.WriteLine();

        await TestDifferentSeats(trip.Id, seatsForTest2);

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas do RabbitMQ apos o Teste 2:");
        Console.WriteLine("   Fila: reservation.created - deve ter +4 mensagens");
        Console.WriteLine("   Fila: reservation.confirmed - deve estar vazia");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("Atualizando lista de assentos disponiveis apos Teste 2...");
        var tripsAfterTest2 = await GetTripsWithAvailableSeats();
        var tripAfterTest2 = tripsAfterTest2.FirstOrDefault(t => t.Id == trip.Id);

        if (tripAfterTest2 == null || tripAfterTest2.AvailableSeats == 0)
        {
            Console.WriteLine("ERRO: Nenhum assento disponivel para o Teste 3!");
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        var seatForTest3 = tripAfterTest2.SeatNumbers.FirstOrDefault();
        if (string.IsNullOrEmpty(seatForTest3))
        {
            Console.WriteLine("ERRO: Nenhum assento disponivel para o Teste 3!");
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   TESTE 3: CONFIRMACAO CONCORRENTE");
        Console.WriteLine("   5 usuarios tentando confirmar a mesma reserva");
        Console.WriteLine("   Assento disponivel: " + seatForTest3);
        Console.WriteLine("========================================");
        Console.WriteLine();

        await TestConcurrentConfirmations(trip.Id, new List<string> { seatForTest3 });

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas do RabbitMQ apos o Teste 3:");
        Console.WriteLine("   Fila: reservation.confirmed - deve ter 1 mensagem (apos pagamento)");
        Console.WriteLine("   Fila: ticket.generated - deve ter 1 mensagem (bilhete gerado)");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para finalizar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   TESTES FINALIZADOS");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Resumo:");
        Console.WriteLine("   - Teste 1: 10 usuarios, 1 reserva (concorrencia)");
        Console.WriteLine("   - Teste 2: 4 usuarios, 4 reservas (assentos diferentes)");
        Console.WriteLine("   - Teste 3: 5 usuarios, 1 confirmacao (concorrencia)");
        Console.WriteLine();
        Console.WriteLine("Verifique as filas do RabbitMQ para ver os eventos processados.");
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }

    static async Task<bool> WaitForApiReady(string baseUrl)
    {
        for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                Console.Write("   Tentativa " + attempt + "/" + MAX_RETRY_ATTEMPTS + "... ");
                var response = await _httpClient.GetAsync("/api/buses");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("OK");
                    return true;
                }

                Console.WriteLine("Status: " + response.StatusCode);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("API nao disponivel");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Timeout");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }

            if (attempt < MAX_RETRY_ATTEMPTS)
            {
                await Task.Delay(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS));
            }
        }

        return false;
    }

    static async Task CheckServicesStatus()
    {
        Console.WriteLine("Verificando status dos servicos...");
        Console.WriteLine();

        try
        {
            var redisResponse = await _httpClient.GetAsync("/api/diagnostics/health");
            if (redisResponse.IsSuccessStatusCode)
            {
                var result = await redisResponse.Content.ReadFromJsonAsync<RedisHealthResponse>();
                if (result != null && result.Data != null)
                {
                    Console.WriteLine("Redis:");
                    Console.WriteLine("   Habilitado: " + result.Data.RedisEnabled);
                    Console.WriteLine("   Conectado: " + result.Data.RedisConnected);
                    if (result.Data.RedisConnected)
                    {
                        Console.WriteLine("   Status: OK");
                    }
                    else
                    {
                        Console.WriteLine("   Status: OFFLINE");
                    }
                }
            }
            else
            {
                Console.WriteLine("Redis: Nao foi possivel verificar (endpoint nao disponivel)");
            }
        }
        catch
        {
            Console.WriteLine("Redis: Nao disponivel");
        }

        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            await tcpClient.ConnectAsync("localhost", 5672);
            Console.WriteLine("RabbitMQ: Conectado (porta 5672)");
            tcpClient.Close();
        }
        catch
        {
            Console.WriteLine("RabbitMQ: Nao disponivel (porta 5672)");
        }

        Console.WriteLine();
    }

    static async Task<List<TripWithSeats>> GetTripsWithAvailableSeats()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/trips");
            if (!response.IsSuccessStatusCode)
            {
                return new List<TripWithSeats>();
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TripResponse>>>();
            if (result == null || result.Data == null || result.Data.Count == 0)
            {
                return new List<TripWithSeats>();
            }

            var tripsWithSeats = new List<TripWithSeats>();

            foreach (var trip in result.Data)
            {
                var seatsResponse = await _httpClient.GetAsync("/api/seats/trip/" + trip.Id);
                if (seatsResponse.IsSuccessStatusCode)
                {
                    var seatsResult = await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<List<SeatResponse>>>();
                    if (seatsResult != null && seatsResult.Data != null)
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
                                SeatNumber = availableSeats.First(),
                                SeatNumbers = availableSeats,
                                AvailableSeats = availableSeats.Count
                            });
                        }
                    }
                }
            }

            return tripsWithSeats;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao buscar viagens: " + ex.Message);
            return new List<TripWithSeats>();
        }
    }

    static string GenerateUniqueDocument()
    {
        var document = new char[11];
        for (int i = 0; i < 11; i++)
        {
            document[i] = (char)('0' + _random.Next(0, 10));
        }
        return new string(document);
    }

    static (string Name, string Document, string Email) GenerateUniquePassenger(int userId)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var document = GenerateUniqueDocument();

        return (
            Name: "Usuario_" + userId + "_" + uniqueId,
            Document: document,
            Email: "usuario" + userId + "_" + uniqueId + "@email.com"
        );
    }

    static async Task TestSameSeat(string tripId, string seatNumber)
    {
        Console.WriteLine("TripId: " + tripId);
        Console.WriteLine("Assento: " + seatNumber);
        Console.WriteLine();

        var tasks = new List<Task<(int UserId, bool Success, string Message, string StatusCode, string ErrorDetail)>>();

        for (int i = 1; i <= 10; i++)
        {
            var userId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var passenger = GenerateUniquePassenger(userId);

                    var request = new
                    {
                        TripId = tripId,
                        Passenger = new
                        {
                            Name = passenger.Name,
                            Document = passenger.Document,
                            Email = passenger.Email,
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

                    if (response.IsSuccessStatusCode)
                    {
                        return (userId, true, "Reserva criada", response.StatusCode.ToString(), "");
                    }
                    else
                    {
                        return (userId, false, "Falha", response.StatusCode.ToString(), responseBody);
                    }
                }
                catch (Exception ex)
                {
                    return (userId, false, "Excecao: " + ex.Message, "Error", "");
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results.OrderBy(r => r.UserId))
        {
            if (result.Success)
            {
                Console.WriteLine("Usuario " + result.UserId + ": SUCESSO - " + result.Message);
            }
            else
            {
                Console.WriteLine("Usuario " + result.UserId + ": FALHA - " + result.Message + " (" + result.StatusCode + ")");
                if (!string.IsNullOrEmpty(result.ErrorDetail))
                {
                    var error = result.ErrorDetail.Length > 200 ? result.ErrorDetail.Substring(0, 200) + "..." : result.ErrorDetail;
                    Console.WriteLine("   Detalhe: " + error);
                }
            }
        }

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        Console.WriteLine();
        Console.WriteLine("Resultado: " + successCount + " sucesso(s), " + failureCount + " falha(s)");
        Console.WriteLine("Esperado: 1 sucesso, 9 falhas");
    }

    static async Task TestDifferentSeats(string tripId, List<string> seatNumbers)
    {
        Console.WriteLine("TripId: " + tripId);
        Console.WriteLine("Assentos: " + string.Join(", ", seatNumbers));
        Console.WriteLine("Cada usuario tem CPF unico e tenta reservar um assento diferente");
        Console.WriteLine();

        var tasks = new List<Task<(int UserId, bool Success, string Message, string StatusCode, string ErrorDetail)>>();

        for (int i = 0; i < seatNumbers.Count; i++)
        {
            var userId = i + 1;
            var seat = seatNumbers[i];
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var passenger = GenerateUniquePassenger(userId + 100);

                    var request = new
                    {
                        TripId = tripId,
                        Passenger = new
                        {
                            Name = passenger.Name,
                            Document = passenger.Document,
                            Email = passenger.Email,
                            Phone = "11999999999"
                        },
                        SeatNumbers = new[] { seat }
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(request, _jsonOptions),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PostAsync("/api/reservations", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return (userId, true, "Assento " + seat + " reservado", response.StatusCode.ToString(), "");
                    }
                    else
                    {
                        return (userId, false, "Erro " + seat, response.StatusCode.ToString(), responseBody);
                    }
                }
                catch (Exception ex)
                {
                    return (userId, false, "Excecao: " + ex.Message, "Error", "");
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results.OrderBy(r => r.UserId))
        {
            if (result.Success)
            {
                Console.WriteLine("Usuario " + result.UserId + ": SUCESSO - " + result.Message);
            }
            else
            {
                Console.WriteLine("Usuario " + result.UserId + ": FALHA - " + result.Message + " (" + result.StatusCode + ")");
                if (!string.IsNullOrEmpty(result.ErrorDetail))
                {
                    var error = result.ErrorDetail.Length > 200 ? result.ErrorDetail.Substring(0, 200) + "..." : result.ErrorDetail;
                    Console.WriteLine("   Detalhe: " + error);
                }
            }
        }

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        Console.WriteLine();
        Console.WriteLine("Resultado: " + successCount + " sucesso(s), " + failureCount + " falha(s)");
        Console.WriteLine("Esperado: " + seatNumbers.Count + " sucessos (todos assentos diferentes)");
    }

    static async Task TestConcurrentConfirmations(string tripId, List<string> seatNumbers)
    {
        var seatNumber = seatNumbers.FirstOrDefault();
        if (string.IsNullOrEmpty(seatNumber))
        {
            Console.WriteLine("ERRO: Nenhum assento disponivel para o teste de confirmacao");
            return;
        }

        Console.WriteLine("TripId: " + tripId);
        Console.WriteLine("Assento para reserva: " + seatNumber);
        Console.WriteLine();

        Console.WriteLine("Criando reserva para teste com usuario unico...");
        var passenger = GenerateUniquePassenger(999);
        var reservationId = await CreateReservation(tripId, seatNumber, passenger.Name, passenger.Document);

        if (string.IsNullOrEmpty(reservationId))
        {
            Console.WriteLine("ERRO: Nao foi possivel criar a reserva para teste");
            return;
        }

        Console.WriteLine("Reserva criada: " + reservationId);
        Console.WriteLine();
        Console.WriteLine("5 usuarios tentando confirmar a mesma reserva simultaneamente...");
        Console.WriteLine();

        var tasks = new List<Task<(int Attempt, bool Success, string Message, string StatusCode, string ErrorDetail)>>();

        for (int i = 1; i <= 5; i++)
        {
            var attempt = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var request = new
                    {
                        ReservationId = reservationId,
                        PaymentMethod = "Cartao de Credito",
                        Observations = "Tentativa " + attempt
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(request, _jsonOptions),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PutAsync("/api/reservations/" + reservationId + "/confirm", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return (attempt, true, "Confirmacao realizada com sucesso", response.StatusCode.ToString(), "");
                    }
                    else
                    {
                        return (attempt, false, "Erro: " + response.StatusCode, response.StatusCode.ToString(), responseBody);
                    }
                }
                catch (Exception ex)
                {
                    return (attempt, false, "Excecao: " + ex.Message, "Error", "");
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var result in results.OrderBy(r => r.Attempt))
        {
            if (result.Success)
            {
                Console.WriteLine("Tentativa " + result.Attempt + ": SUCESSO - " + result.Message);
            }
            else
            {
                Console.WriteLine("Tentativa " + result.Attempt + ": FALHA - " + result.Message);
                if (!string.IsNullOrEmpty(result.ErrorDetail))
                {
                    var error = result.ErrorDetail.Length > 200 ? result.ErrorDetail.Substring(0, 200) + "..." : result.ErrorDetail;
                    Console.WriteLine("   Detalhe: " + error);
                }
            }
        }

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        Console.WriteLine();
        Console.WriteLine("Resultado: " + successCount + " sucesso(s), " + failureCount + " falha(s)");
        Console.WriteLine("Esperado: 1 sucesso (apenas 1 consegue confirmar), 4 falhas");
    }

    static async Task<string> CreateReservation(string tripId, string seatNumber, string name, string document)
    {
        try
        {
            var request = new
            {
                TripId = tripId,
                Passenger = new
                {
                    Name = name,
                    Document = document,
                    Email = name.ToLower().Replace(" ", "") + "@email.com",
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
                Console.WriteLine("Erro ao criar reserva: " + responseBody);
                return null;
            }

            var result = JsonSerializer.Deserialize<ApiResponse<ReservationResponse>>(responseBody, _jsonOptions);
            if (result != null && result.Data != null)
            {
                return result.Data.Id.ToString();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Excecao ao criar reserva: " + ex.Message);
            return null;
        }
    }

    public class RedisHealthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RedisHealthData? Data { get; set; }
    }

    public class RedisHealthData
    {
        public bool RedisEnabled { get; set; }
        public bool RedisConnected { get; set; }
        public string RedisConfiguration { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ReservationResponse
    {
        public Guid Id { get; set; }
    }

    public class TripResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }

    public class SeatResponse
    {
        public string Number { get; set; } = string.Empty;
        public int Status { get; set; }
    }

    public class TripWithSeats
    {
        public string Id { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public List<string> SeatNumbers { get; set; } = new List<string>();
        public int AvailableSeats { get; set; }
    }
}