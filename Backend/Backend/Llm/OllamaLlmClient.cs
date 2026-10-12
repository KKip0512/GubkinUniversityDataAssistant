using Microsoft.Extensions.Options;

namespace Backend.Llm
{
    public class OllamaLlmClient(HttpClient http, IOptions<LlmOptions> options, OllamaMessagesBuilder ollamaMessagesBuilder) : ILlmClient
    {
        public async Task<string> GenerateSqlAsync(string question, string? previousSql = null, string? previousError = null, CancellationToken ct = default)
        {
            var request = new OllamaChatRequest(
                Model: options.Value.Model,
                Messages: ollamaMessagesBuilder.Build(question, previousSql, previousError),
                Stream: false,
                KeepAlive: $"{options.Value.KeepAliveMinutes}m",
                Options: new OllamaOptions(Temperature: 0, options.Value.ContextSize));

            using var response = await http.PostAsJsonAsync("/api/chat", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Ollama вернула {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
            var text = result?.Message?.Content;

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Модель вернула пустой ответ.");

            return text;
        }
    }
}