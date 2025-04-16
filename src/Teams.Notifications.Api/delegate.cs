using System.Collections;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Teams.Notifications.Api;

public class RequestAndResponseLoggerHandler : DelegatingHandler
{
    private const int MaxBodyLength = 10000; // we don't want to log  big request or responses
    private readonly ILogger<RequestAndResponseLoggerHandler> _logger;

    public RequestAndResponseLoggerHandler(ILogger<RequestAndResponseLoggerHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var timer = Stopwatch.StartNew();
        var requestBody = string.Empty;
        if (request?.Content != null) requestBody = await request.Content?.ReadAsStringAsync(cancellationToken)!;
        if (requestBody.Length > MaxBodyLength)
            requestBody = $"body to big:{requestBody.Length} chars";

        var requestLogEvent = new
        {
            url = request?.RequestUri?.OriginalString,
            method = request?.Method.Method,
            body = requestBody,
            headers = FilterHeader(request?.Headers)
        };

        if (request == null) return null;
        var response = await base.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (responseBody.Length > MaxBodyLength)
            responseBody = $"body to big:{responseBody.Length} chars]";

        var responseLogEvent = new
        {
            status = (int)response.StatusCode,
            body = responseBody,
            headers = FilterHeader(response.Headers),
            took = new { total = timer.Elapsed }
        };

        _logger.LogDebug("Request finished and got a {status}: {requestLogEvent} {responseLogEvent}",
            responseLogEvent.status,
            requestLogEvent,
            responseLogEvent);
        return response;
    }

    private static string FilterHeader(IEnumerable headers)
    {
        var headersString = headers.ToString()!.Replace("\n", ",").Replace("\r", ""); // instead of an enter we want comma's
        var regex = new Regex(@"Bearer(.*?),"); // remove the bearer part, long stuff that we don't need
        return regex.Replace(headersString, "OMITTED, ");
    }
}
