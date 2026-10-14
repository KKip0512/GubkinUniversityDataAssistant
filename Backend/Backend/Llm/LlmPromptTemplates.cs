using YamlDotNet.Serialization;

namespace Backend.Llm
{
    public class PromptTemplates
    {
        [YamlMember(Alias = "systemPrompt")]
        public required string System { get; init; }

        [YamlMember(Alias = "examplesPrompt")]
        public required List<PromptExample> Examples { get; init; }

        [YamlMember(Alias = "retryPrompt")]
        public required string Retry { get; init; }
    }

    public class PromptExample
    {
        [YamlMember(Alias = "question")]
        public required string Question { get; init; }

        [YamlMember(Alias = "sql")]
        public required string Sql { get; init; }
    }
}
