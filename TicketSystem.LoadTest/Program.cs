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
        Console.WriteLine(" TESTE DE CONCORRENCIA - RESERVAS");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.WriteLine("ATENCAO: Durante o teste, pausas serao feitas para:");
        Console.WriteLine(" - Verificar filas no RabbitMQ (http://localhost:15672)");
        Console.WriteLine(" - Verificar cache no Redis (redis-cli)");
        Console.WriteLine(" - Verificar dados no SQL Server");
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

        var validTrips = trips.Where(t => t.AvailableSeats >= 8).ToList();
        if (validTrips.Count == 0)
        {
            Console.WriteLine("ERRO: Nenhuma viagem com pelo menos 8 assentos disponiveis!");
            Console.WriteLine("Buscando viagens com pelo menos 5 assentos disponiveis...");

            validTrips = trips.Where(t => t.AvailableSeats >= 5).ToList();
            if (validTrips.Count == 0)
            {
                Console.WriteLine("ERRO: Nenhuma viagem com assentos suficientes!");
                Console.WriteLine();
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
                return;
            }
        }

        var randomIndex = _random.Next(0, validTrips.Count);
        var trip = validTrips[randomIndex];

        var seatForTest1 = trip.SeatNumbers.FirstOrDefault();

        var seatsForTest2 = trip.SeatNumbers
            .Where(s => s != seatForTest1)
            .Take(4)
            .ToList();

        var usedSeats = new List<string> { seatForTest1 };
        usedSeats.AddRange(seatsForTest2);

        var seatForTest3 = trip.SeatNumbers
            .Where(s => !usedSeats.Contains(s))
            .FirstOrDefault();

        if (string.IsNullOrEmpty(seatForTest1))
        {
            Console.WriteLine("ERRO: Nenhum assento disponivel para o Teste 1!");
            Console.ReadKey();
            return;
        }

        if (seatsForTest2.Count < 4)
        {
            Console.WriteLine("AVISO: Poucos assentos disponiveis para o Teste 2. Usando " + seatsForTest2.Count + " assentos.");
        }

        Console.WriteLine("Viagem encontrada (aleatoria): " + trip.Id);
        Console.WriteLine(" Origem: " + trip.Origin);
        Console.WriteLine(" Destino: " + trip.Destination);
        Console.WriteLine(" Assentos disponiveis: " + trip.AvailableSeats);
        Console.WriteLine(" Assento para Teste 1: " + seatForTest1);
        Console.WriteLine(" Assentos para Teste 2: " + string.Join(", ", seatsForTest2));
        Console.WriteLine(" Assento para Teste 3: " + (seatForTest3 ?? "Nenhum disponivel"));

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas no RabbitMQ antes de comecar:");
        Console.WriteLine(" http://localhost:15672");
        Console.WriteLine(" Usuario: guest / Senha: guest");
        Console.WriteLine(" Filas: reservation.created, reservation.confirmed, payment.failed, ticket.generated");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER quando estiver pronto para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" TESTE 1: MESMO ASSENTO");
        Console.WriteLine(" 10 usuarios tentando reservar o assento " + seatForTest1);
        Console.WriteLine("========================================");
        Console.WriteLine();

        await TestSameSeat(trip.Id, seatForTest1);

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas do RabbitMQ apos o Teste 1:");
        Console.WriteLine(" Fila: reservation.created - deve ter 1 mensagem");
        Console.WriteLine(" Fila: reservation.confirmed - deve estar vazia (consumidor processou)");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" TESTE 2: ASSENTOS DIFERENTES");
        Console.WriteLine(" " + seatsForTest2.Count + " usuarios diferentes reservando assentos diferentes");
        Console.WriteLine(" Assentos: " + string.Join(", ", seatsForTest2));
        Console.WriteLine("========================================");
        Console.WriteLine();

        await TestDifferentSeats(trip.Id, seatsForTest2);

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas do RabbitMQ apos o Teste 2:");
        Console.WriteLine(" Fila: reservation.created - deve ter +" + seatsForTest2.Count + " mensagens");
        Console.WriteLine(" Fila: reservation.confirmed - deve estar vazia");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" TESTE 3: PAGAMENTO CONCORRENTE COM RESERVAS EXPIRADAS");
        Console.WriteLine("========================================");
        Console.WriteLine();

        if (!string.IsNullOrEmpty(seatForTest3))
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(" TESTE 3: PAGAMENTO CONCORRENTE COM RESERVAS EXPIRADAS");
            Console.WriteLine(" 5 usuarios com reservas para o mesmo assento, mas apenas 1 esta valida");
            Console.WriteLine(" Assento: " + seatForTest3);
            Console.WriteLine("========================================");
            Console.WriteLine();

            await TestConcurrentPaymentsWithExpiredReservations(trip.Id, seatForTest3);
        }
        else
        {
            Console.WriteLine("AVISO: Nenhum assento disponivel para o Teste 3.");
            Console.WriteLine("Pulando Teste 3...");
        }

        Console.WriteLine();
        Console.WriteLine("PAUSA: Verifique as filas do RabbitMQ apos o Teste 3:");
        Console.WriteLine(" Fila: reservation.confirmed - deve ter 1 mensagem (apos pagamento)");
        Console.WriteLine(" Fila: ticket.generated - deve ter 1 mensagem (bilhete gerado)");
        Console.WriteLine();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" PAGAMENTO E FLUXO COMPLETO");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.WriteLine("Deseja simular o pagamento para uma das reservas criadas?");
        Console.WriteLine(" [S] Sim - Processar pagamento e gerar bilhete");
        Console.WriteLine(" [N] Nao - Finalizar sem pagamento");
        Console.Write("Escolha uma opcao (S/N): ");

        var option = Console.ReadLine()?.ToUpper();

        if (option == "S")
        {
            Console.WriteLine();
            Console.WriteLine("Buscando reservas pendentes...");

            try
            {
                var reservationsResponse = await _httpClient.GetAsync("/api/reservations");
                if (reservationsResponse.IsSuccessStatusCode)
                {
                    var result = await reservationsResponse.Content.ReadFromJsonAsync<ApiResponse<List<ReservationDto>>>();

                    var allReservations = result?.Data?.ToList() ?? new List<ReservationDto>();
                    var pendingReservations = allReservations.Where(r => r.Status == 0).ToList();

                    if (pendingReservations.Count == 0 && allReservations.Count > 0)
                    {
                        Console.WriteLine("Nenhuma reserva pendente encontrada. Todas as reservas ja foram processadas.");
                        Console.WriteLine("Reservas existentes: " + allReservations.Count);
                    }
                    else if (pendingReservations.Count == 0)
                    {
                        Console.WriteLine("Nenhuma reserva pendente encontrada.");
                    }
                    else
                    {
                        Console.WriteLine("Reservas pendentes encontradas: " + pendingReservations.Count);

                        foreach (var reservation in pendingReservations)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Processando pagamento para reserva: " + reservation.Id);
                            Console.WriteLine(" Passageiro: " + reservation.PassengerName);
                            Console.WriteLine(" Total: R$ " + reservation.TotalAmount);

                            var payRequest = new { paymentMethod = "Cartao de Credito" };
                            var payContent = new StringContent(
                                JsonSerializer.Serialize(payRequest, _jsonOptions),
                                Encoding.UTF8,
                                "application/json");

                            var payResponse = await _httpClient.PostAsync("/api/payment/reservations/" + reservation.Id + "/pay", payContent);

                            if (payResponse.IsSuccessStatusCode)
                            {
                                var responseContent = await payResponse.Content.ReadAsStringAsync();
                                Console.WriteLine(" Pagamento aprovado!");
                                Console.WriteLine(" Email enviado para: " + reservation.PassengerEmail);
                                Console.WriteLine(" Bilhete gerado com sucesso");
                            }
                            else
                            {
                                var error = await payResponse.Content.ReadAsStringAsync();
                                Console.WriteLine(" Pagamento recusado: " + error);
                            }

                            await Task.Delay(500);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Erro ao buscar reservas: " + reservationsResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao processar pagamentos: " + ex.Message);
            }
        }
        else
        {
            Console.WriteLine("Pagamento nao processado. Os bilhetes nao serao gerados.");
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" TESTES FINALIZADOS");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }

    // ============================================
    // TESTE 3: PAGAMENTO CONCORRENTE COM RESERVAS EXPIRADAS
    // ============================================
    static async Task TestConcurrentPaymentsWithExpiredReservations(string tripId, string seatNumber)
    {
        Console.WriteLine("TripId: " + tripId);
        Console.WriteLine("Assento: " + seatNumber);
        Console.WriteLine();
        Console.WriteLine("Criando 5 reservas para o mesmo assento, com tempos de expiracao diferentes...");
        Console.WriteLine(" Apenas 1 reserva ficara valida (a ultima criada)");
        Console.WriteLine(" As outras 4 serao criadas com expiracao de 3 segundos (irao expirar)");
        Console.WriteLine();

        var reservations = new List<(Guid Id, string PassengerName, string PassengerEmail)>();

        // Criar 4 reservas que vão expirar rapidamente (3 segundos) usando o endpoint de teste
        for (int i = 1; i <= 4; i++)
        {
            var passenger = GenerateUniquePassenger(2000 + i);

            Console.WriteLine($"Criando reserva {i} (EXPIRA EM 3 SEGUNDOS)...");

            var reservationId = await CreateTestReservation(tripId, seatNumber, passenger.Name, passenger.Document, 3);

            if (!string.IsNullOrEmpty(reservationId))
            {
                reservations.Add((Guid.Parse(reservationId), passenger.Name, passenger.Email));
                Console.WriteLine($" Reserva {i} criada: {reservationId}");
            }
            else
            {
                Console.WriteLine($" Reserva {i} falhou ao criar (assento pode estar ocupado)");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Aguardando 6 segundos para as reservas expirarem e os assentos serem liberados...");
        Console.WriteLine("(O ReservationExpirationWorker deve processar neste intervalo)");
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Verificar se o assento foi liberado
        Console.WriteLine();
        Console.WriteLine($"Verificando se o assento {seatNumber} foi liberado...");

        try
        {
            var seatsResponse = await _httpClient.GetAsync($"/api/seats/trip/{tripId}");
            if (seatsResponse.IsSuccessStatusCode)
            {
                var seatsResult = await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<List<SeatResponse>>>();
                var seat = seatsResult?.Data?.FirstOrDefault(s => s.Number == seatNumber);

                if (seat != null)
                {
                    Console.WriteLine($" Status do assento {seatNumber}: {(seat.Status == 0 ? "Disponivel" : "Ocupado (Status: " + seat.Status + ")")}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao verificar assento: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Criando reserva 5 (VALIDA - expira em 15 minutos)...");
        var validPassenger = GenerateUniquePassenger(2005);
        var validReservationId = await CreateReservation(tripId, seatNumber, validPassenger.Name, validPassenger.Document);

        if (!string.IsNullOrEmpty(validReservationId))
        {
            reservations.Add((Guid.Parse(validReservationId), validPassenger.Name, validPassenger.Email));
            Console.WriteLine($" Reserva 5 criada (valida): {validReservationId}");
        }
        else
        {
            Console.WriteLine("ERRO: Nao foi possivel criar a reserva valida. O assento ainda esta ocupado.");
            Console.WriteLine("Teste 3 nao pode ser concluido.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" 5 usuarios tentando pagar pela reserva do mesmo assento");
        Console.WriteLine(" Apenas a reserva 5 esta valida (nao expirada)");
        Console.WriteLine(" As reservas 1 a 4 ja expiraram");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var tasks = new List<Task<(int Index, string ReservationId, string PassengerName, bool Success, string Message)>>();

        foreach (var reservation in reservations)
        {
            var index = reservations.IndexOf(reservation) + 1;
            var reservationId = reservation.Id;
            var passengerName = reservation.PassengerName;

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var payRequest = new { paymentMethod = "Cartao de Credito" };
                    var payContent = new StringContent(
                        JsonSerializer.Serialize(payRequest, _jsonOptions),
                        Encoding.UTF8,
                        "application/json");

                    Console.WriteLine($" Tentando pagamento para reserva {index} (Id: {reservationId})...");

                    var response = await _httpClient.PostAsync("/api/payment/reservations/" + reservationId + "/pay", payContent);

                    if (response.IsSuccessStatusCode)
                    {
                        return (index, reservationId.ToString(), passengerName, true, "Pagamento aprovado!");
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        return (index, reservationId.ToString(), passengerName, false, "Falha no pagamento: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    return (index, reservationId.ToString(), passengerName, false, "Erro: " + ex.Message);
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        Console.WriteLine();
        Console.WriteLine("=== RESULTADOS DO TESTE 3 ===");
        Console.WriteLine();

        foreach (var result in results.OrderBy(r => r.Index))
        {
            if (result.Success)
            {
                Console.WriteLine($" Reserva {result.Index} ({result.PassengerName}): SUCESSO - {result.Message}");
            }
            else
            {
                Console.WriteLine($" Reserva {result.Index} ({result.PassengerName}): FALHA - {result.Message}");
            }
        }

        var successCount = results.Count(r => r.Success);
        Console.WriteLine();
        Console.WriteLine("Resultado final: " + successCount + " sucesso(s), " + (results.Length - successCount) + " falha(s)");
        Console.WriteLine("Esperado: 1 sucesso (apenas a reserva 5, que nao expirou)");
        Console.WriteLine();
        Console.WriteLine("O email e o bilhete devem ser gerados APENAS para a reserva 5 (valida)");
    }

    // ============================================
    // CRIAR RESERVA COM EXPIRACAO PERSONALIZADA (USA ENDPOINT DE TESTE)
    // ============================================
    static async Task<string> CreateTestReservation(string tripId, string seatNumber, string name, string document, int expirationSeconds)
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

            // Usar o endpoint de teste com expiracao personalizada
            var response = await _httpClient.PostAsync($"/api/reservations/test?expirationSeconds={expirationSeconds}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao criar reserva de teste: " + responseBody);
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
            Console.WriteLine("Excecao ao criar reserva de teste: " + ex.Message);
            return null;
        }
    }

    static async Task<string> CreateReservationWithCustomExpiration(string tripId, string seatNumber, string name, string document, TimeSpan expirationTime)
    {
        // Este metodo agora usa o endpoint de teste
        return await CreateTestReservation(tripId, seatNumber, name, document, (int)expirationTime.TotalSeconds);
    }

    static async Task<bool> WaitForApiReady(string baseUrl)
    {
        for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                Console.Write(" Tentativa " + attempt + "/" + MAX_RETRY_ATTEMPTS + "... ");
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
                    Console.WriteLine(" Habilitado: " + result.Data.RedisEnabled);
                    Console.WriteLine(" Conectado: " + result.Data.RedisConnected);
                    if (result.Data.RedisConnected)
                    {
                        Console.WriteLine(" Status: OK");
                    }
                    else
                    {
                        Console.WriteLine(" Status: OFFLINE");
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
            Name: "Usuario_" + userId + "" + uniqueId,
            Document: document,
            Email: "usuario" + userId + "" + uniqueId + "@email.com"
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
                    Console.WriteLine(" Detalhe: " + error);
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
                    Console.WriteLine(" Detalhe: " + error);
                }
            }
        }

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        Console.WriteLine();
        Console.WriteLine("Resultado: " + successCount + " sucesso(s), " + failureCount + " falha(s)");
        Console.WriteLine("Esperado: " + seatNumbers.Count + " sucessos (todos assentos diferentes)");
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

    // ============================================
    // CLASSES DE RESPOSTA
    // ============================================

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

    public class ReservationDto
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
