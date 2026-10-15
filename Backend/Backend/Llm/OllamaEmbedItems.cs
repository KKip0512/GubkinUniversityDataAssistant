using System.Text.Json.Serialization;

namespace Backend.Llm
{
    public record OllamaEmbedOptions(
        [property: JsonPropertyName("num_gpu")] int NumGpu);

    public record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input,
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
        [property: JsonPropertyName("options")] OllamaEmbedOptions Options);

    public record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] IReadOnlyList<float[]>? Embeddings);
}
