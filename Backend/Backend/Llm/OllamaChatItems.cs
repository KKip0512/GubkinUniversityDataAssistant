using System.Text.Json.Serialization;

namespace Backend.Llm
{
    public static class OllamaMessageRole
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
    }

    public record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    public record OllamaOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_ctx")] int NumCtx);

    public record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
        [property: JsonPropertyName("options")] OllamaOptions Options);

    public record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message);
}
