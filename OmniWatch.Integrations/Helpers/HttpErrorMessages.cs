using System.Net;

namespace OmniWatch.Integrations.Helpers
{
    public static class HttpErrorMessages
    {
        private static readonly Dictionary<HttpStatusCode, string> _messages = new()
    {
        // 1xx — Informational
        { HttpStatusCode.Continue, "The server acknowledges the request. Continue sending the body." },
        { HttpStatusCode.SwitchingProtocols, "The server is switching protocols as requested." },
        { HttpStatusCode.Processing, "The server is processing the request." },

        // 2xx — Success
        { HttpStatusCode.OK, "The request completed successfully." },
        { HttpStatusCode.Created, "The resource was created successfully." },
        { HttpStatusCode.Accepted, "The request was accepted and is being processed." },
        { HttpStatusCode.NonAuthoritativeInformation, "The response contains modified metadata." },
        { HttpStatusCode.NoContent, "The request succeeded but returned no content." },
        { HttpStatusCode.ResetContent, "The request succeeded; reset the document view." },
        { HttpStatusCode.PartialContent, "The server returned partial content." },

        // 3xx — Redirection
        { HttpStatusCode.MultipleChoices, "Multiple options are available for this resource." },
        { HttpStatusCode.MovedPermanently, "The resource has been moved permanently." },
        { HttpStatusCode.Found, "The resource has been temporarily moved." },
        { HttpStatusCode.SeeOther, "See another resource for the response." },
        { HttpStatusCode.NotModified, "The resource has not changed since the last request." },
        { HttpStatusCode.UseProxy, "The resource must be accessed through a proxy." },
        { HttpStatusCode.TemporaryRedirect, "The resource is temporarily located elsewhere." },
        { HttpStatusCode.PermanentRedirect, "The resource has been permanently redirected." },

        // 4xx — Client Errors
        { HttpStatusCode.BadRequest, "The request was invalid or malformed." },
        { HttpStatusCode.Unauthorized, "Authentication failed. Check your credentials." },
        { HttpStatusCode.PaymentRequired, "Payment is required to access this resource." },
        { HttpStatusCode.Forbidden, "Access denied. You do not have permission." },
        { HttpStatusCode.NotFound, "The requested resource was not found." },
        { HttpStatusCode.MethodNotAllowed, "The HTTP method is not allowed for this resource." },
        { HttpStatusCode.NotAcceptable, "The server cannot return data in the requested format." },
        { HttpStatusCode.ProxyAuthenticationRequired, "Proxy authentication is required." },
        { HttpStatusCode.RequestTimeout, "The request timed out." },
        { HttpStatusCode.Conflict, "The request conflicts with the current state of the resource." },
        { HttpStatusCode.Gone, "The resource is no longer available." },
        { HttpStatusCode.LengthRequired, "Content-Length header is required." },
        { HttpStatusCode.PreconditionFailed, "A precondition in the request failed." },
        { HttpStatusCode.RequestEntityTooLarge, "The request payload is too large." },
        { HttpStatusCode.RequestUriTooLong, "The request URI is too long." },
        { HttpStatusCode.UnsupportedMediaType, "The media type is not supported." },
        { HttpStatusCode.RequestedRangeNotSatisfiable, "The requested range is invalid." },
        { HttpStatusCode.ExpectationFailed, "The server cannot meet the Expect header requirements." },
        { HttpStatusCode.MisdirectedRequest, "The request was sent to the wrong server." },
        { HttpStatusCode.UnprocessableEntity, "The server cannot process the request content." },
        { HttpStatusCode.Locked, "The resource is locked." },
        { HttpStatusCode.FailedDependency, "The request failed due to a dependency error." },
        { HttpStatusCode.EarlyHints, "The request is too early to be processed safely." },
        { HttpStatusCode.UpgradeRequired, "Upgrade to a different protocol is required." },
        { HttpStatusCode.PreconditionRequired, "A precondition header is required." },
        { HttpStatusCode.TooManyRequests, "Rate limit exceeded. Slow down your requests." },
        { HttpStatusCode.RequestHeaderFieldsTooLarge, "The request headers are too large." },
        { HttpStatusCode.UnavailableForLegalReasons, "The resource is unavailable for legal reasons." },

        // 5xx — Server Errors
        { HttpStatusCode.InternalServerError, "The server encountered an unexpected error." },
        { HttpStatusCode.NotImplemented, "The server does not support this functionality." },
        { HttpStatusCode.BadGateway, "Bad gateway. The upstream server failed." },
        { HttpStatusCode.ServiceUnavailable, "Service unavailable. Try again later." },
        { HttpStatusCode.GatewayTimeout, "The server took too long to respond." },
        { HttpStatusCode.HttpVersionNotSupported, "The HTTP version is not supported." },
        { HttpStatusCode.VariantAlsoNegotiates, "Content negotiation failed." },
        { HttpStatusCode.InsufficientStorage, "The server has insufficient storage." },
        { HttpStatusCode.LoopDetected, "The server detected an infinite loop." },
        { HttpStatusCode.NotExtended, "Further extensions to the request are required." },
        { HttpStatusCode.NetworkAuthenticationRequired, "Network authentication is required." }
    };

        public static string GetMessage(HttpStatusCode code)
            => _messages.TryGetValue(code, out var msg)
                ? msg
                : "An unexpected HTTP error occurred.";
    }

}
