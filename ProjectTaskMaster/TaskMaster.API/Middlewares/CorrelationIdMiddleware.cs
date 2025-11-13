using Serilog.Context;

namespace TaskMaster.API.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetCorrelationId(context);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private string GetCorrelationId(HttpContext context)
        {
            context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId);
            return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
        }
    }
}
