using System.Threading.RateLimiting;
using Backend.Endpoints;
using Backend.Llm;
using Backend.Models;
using Backend.Services;
using Backend.Sql;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddOptions<LlmOptions>()
    .BindConfiguration("Llm")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var llm = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return OllamaMessagesBuilder.FromYaml(Path.Combine(env.ContentRootPath, llm.PromptsPath));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status503ServiceUnavailable;
    options.AddConcurrencyLimiter(QueryEndpoints.RateLimitPolicy, limiter =>
    {
        limiter.PermitLimit = config.GetValue("Query:MaxConcurrentRequests", 2);
        limiter.QueueLimit = config.GetValue("Query:QueueLimit", 20);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddSingleton<SqlValidator>();

builder.Services.AddHttpClient<ILlmClient, OllamaLlmClient>((sp, client) =>
{
    var llm = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
    client.BaseAddress = new Uri(llm.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(llm.TimeoutSeconds);
});

builder.Services.AddSingleton<IQueryExecutor, PostgresQueryExecutor>();
builder.Services.AddSingleton<PostgresSchemaProvider>();

builder.Services.AddScoped<QueryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, message) = error is BadHttpRequestException badRequest
        ? (badRequest.StatusCode, QueryResponse.BadRequestMessage)
        : (StatusCodes.Status500InternalServerError, QueryResponse.UnavailableMessage);

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(QueryResponse.Fail(null, message));
}));

app.UseRateLimiter();

app.MapQueryEndpoints();

app.Run();
