using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints
{
    public static class QueryEndpoints
    {
        public const string RateLimitPolicy = "Query";

        public static void MapQueryEndpoints(this WebApplication app)
        {
            app.MapPost("/api/query", async (QueryRequest request, QueryService service, CancellationToken ct) =>
            {
                var result = await service.AskAsync(request.Question, ct);
                return Results.Ok(result);
            })
            .WithName("Query")
            .WithTags("Query")
            .RequireRateLimiting(RateLimitPolicy);

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
               .WithTags("Service");
        }
    }
}
