using Serilog.Context;

namespace Matchcota.Api.Middleware;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    private const string RequestIdHeader = "X-Request-ID";
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context)
    {
        var requestId = context.Request.Headers[RequestIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers.TryAdd(RequestIdHeader, requestId);
        context.TraceIdentifier = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}
