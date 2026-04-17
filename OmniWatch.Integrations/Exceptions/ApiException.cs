using System.Net;

namespace OmniWatch.Integrations.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public string? ResponseContent { get; }

        public ApiException()
        {
        }

        public ApiException(string message)
            : base(message)
        {
        }

        public ApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ApiException(HttpStatusCode statusCode, string? responseContent)
            : base($"API Error: {(int)statusCode} - {statusCode}")
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
