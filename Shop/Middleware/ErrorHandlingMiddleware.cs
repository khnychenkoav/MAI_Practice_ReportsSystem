using System.Net;

namespace Shop.Middleware
{
    /// <summary>
    /// Middleware for error handling.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        /// <summary>
        /// Constructor to initialize <see cref="ErrorHandlingMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for logging errors.</param>
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Handles the HTTP request.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong: {ex}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        /// <summary>
        /// Handles the exception and returns the appropriate response.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="exception">Exception to handle.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error from the custom middleware.",
                Detailed = exception.Message
            }.ToString());
        }
    }

    /// <summary>
    /// Class representing error details.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Detailed error message.
        /// </summary>
        public string Detailed { get; set; }

        /// <summary>
        /// Converts the object to a JSON string.
        /// </summary>
        /// <returns>JSON string representing the object.</returns>
        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
