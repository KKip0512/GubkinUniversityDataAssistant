using System.ComponentModel.DataAnnotations;

namespace Backend.Llm
{
    public class LlmOptions
    {
        [Required] public required string BaseUrl { get; init; }
        [Required] public required string Model { get; init; }
        [Required] public required string PromptsPath { get; init; }
        [Range(1, 600)] public required int TimeoutSeconds { get; init; }
        [Range(-1, 1440)] public required int KeepAliveMinutes { get; init; }
        [Range(1, 131072)] public required int ContextSize { get; init; }
    }
}
