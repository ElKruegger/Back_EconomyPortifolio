using System.Net;
using System.Text.Json;

namespace EconomyBackPortifolio.Middleware
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
            context.Response.ContentType = "application/json";
            var response = context.Response;

            var errorResponse = new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method
            };

            switch (exception)
            {
                case ArgumentException argEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Message = argEx.Message;
                    errorResponse.Errors = new Dictionary<string, string[]>
                    {
                        { "Argument", new[] { argEx.Message } }
                    };
                    _logger.LogWarning(exception, "Erro de validação: {Message}", argEx.Message);
                    break;

                case InvalidOperationException invOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Message = invOpEx.Message;
                    _logger.LogWarning(exception, "Erro de operação: {Message}", invOpEx.Message);
                    break;

                case UnauthorizedAccessException unauthEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Message = unauthEx.Message;
                    _logger.LogWarning(exception, "Erro de autorização: {Message}", unauthEx.Message);
                    break;

                case KeyNotFoundException keyEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Message = keyEx.Message;
                    _logger.LogWarning(exception, "Recurso não encontrado: {Message}", keyEx.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.StatusCode = response.StatusCode;
                    errorResponse.Message = "Ocorreu um erro interno no servidor. Tente novamente mais tarde.";
                    _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);
                    break;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, options);
            await response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
