using System.Net;
using System.Text.Json;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
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
            _logger.LogError(exception, "Erro na requisicao. Path: {Path}, Method: {Method}",
            context.Request.Path, context.Request.Method);

            var response = context.Response;
            response.ContentType = "application/json";

            var apiResponse = new ApiResponse<object>
            {
                Success = false,
                Timestamp = DateTime.UtcNow
            };

            var errorDetails = new List<string>
{
exception.Message
};

            if (_env.IsDevelopment())
            {
                errorDetails.Add("Tipo: " + exception.GetType().FullName);
                errorDetails.Add("StackTrace: " + exception.StackTrace);
                if (exception.InnerException != null)
                {
                    errorDetails.Add("InnerException: " + exception.InnerException.Message);
                    errorDetails.Add("InnerStackTrace: " + exception.InnerException.StackTrace);
                }
                apiResponse.Errors = errorDetails;
            }
            else
            {
                apiResponse.Errors = new List<string> { "Ocorreu um erro inesperado. Tente novamente mais tarde." };
            }

            switch (exception)
            {
                case KeyNotFoundException _:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    apiResponse.Message = "Recurso nao encontrado";
                    if (!_env.IsDevelopment())
                        apiResponse.Errors = new List<string> { exception.Message };
                    break;

                case InvalidOperationException _:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "Operacao invalida";
                    break;

                case ArgumentException _:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "Argumento invalido";
                    break;

                case FluentValidation.ValidationException ex:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    apiResponse.Message = "Erro de validacao";
                    if (!_env.IsDevelopment())
                        apiResponse.Errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
                    break;

                case UnauthorizedAccessException _:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    apiResponse.Message = "Acesso nao autorizado";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    apiResponse.Message = "Erro interno do servidor";
                    break;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, jsonOptions);
            await response.WriteAsync(jsonResponse);
        }
    }
}

