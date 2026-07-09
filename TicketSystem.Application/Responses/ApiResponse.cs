namespace TicketSystem.Application.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTime.Now;
            Success = true;
        }

        public ApiResponse(T data, string message = "Operação realizada com sucesso") : this()
        {
            Data = data;
            Message = message;
        }

        public ApiResponse(string message, bool success = false)
        {
            Success = success;
            Message = message;
            Timestamp = DateTime.Now;
            Errors = new List<string>();
        }

        public ApiResponse(List<string> errors, string message = "Erro ao processar a requisição")
        {
            Success = false;
            Message = message;
            Errors = errors;
            Timestamp = DateTime.Now;
        }

        public void AddError(string error)
        {
            Errors.Add(error);
            Success = false;
        }
    }
}
