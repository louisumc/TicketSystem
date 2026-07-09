using System.Net;
using System.Text.Json;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "Ocorreu um erro inesperado");

            var response = context.Response;
            response.ContentType = "application/json";

            var apiResponse = new ApiResponse<object>();

            switch (exception)
            {
                case KeyNotFoundException _:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Success = false;
                    apiResponse.Message = "Recurso não encontrado";
                    apiResponse.Errors = new List<string> { exception.Message };
                    break;

                case InvalidOperationException _:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Success = false;
                    apiResponse.Message = "Operação inválida";
                    apiResponse.Errors = new List<string> { exception.Message };
                    break;

                case ArgumentException _:
                case FluentValidation.ValidationException _:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Success = false;
                    apiResponse.Message = "Erro de validação";
                    apiResponse.Errors = new List<string> { exception.Message };
                    break;

                case UnauthorizedAccessException _:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    apiResponse.Success = false;
                    apiResponse.Message = "Acesso não autorizado";
                    apiResponse.Errors = new List<string> { exception.Message };
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    apiResponse.Success = false;
                    apiResponse.Message = "Erro interno do servidor";
                    apiResponse.Errors = new List<string> { "Ocorreu um erro inesperado. Tente novamente mais tarde." };
                    break;
            }

            apiResponse.Timestamp = DateTime.Now;

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(jsonResponse);
        }
    }
}