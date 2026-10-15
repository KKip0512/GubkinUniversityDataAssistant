using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Backend.Llm
{
    public class OllamaMessagesBuilder
    {
        private readonly string _system;
        private readonly string _retryTemplate;

        public OllamaMessagesBuilder(PromptTemplates templates)
        {
            if (string.IsNullOrWhiteSpace(templates.System))
                throw new InvalidOperationException("Prompts: не задан system");
            if (string.IsNullOrWhiteSpace(templates.Retry))
                throw new InvalidOperationException("Prompts: не задан retry");

            _system = templates.System.Trim();
            _retryTemplate = templates.Retry.Trim();
        }

        public static OllamaMessagesBuilder FromYaml(string path)
        {
            var templates = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize<PromptTemplates>(File.ReadAllText(path));

            return new OllamaMessagesBuilder(templates);
        }

        public IReadOnlyList<OllamaMessage> Build(
            string schema, IReadOnlyList<PromptExample> examples, string question, string? previousSql, string? previousError)
        {
            var messages = new List<OllamaMessage> { new(OllamaMessageRole.System, $"{_system}\n\n{schema}") };
            foreach (var example in examples)
            {
                messages.Add(new(OllamaMessageRole.User, example.Question.Trim()));
                messages.Add(new(OllamaMessageRole.Assistant, example.Sql.Trim()));
            }
            messages.Add(new(OllamaMessageRole.User, question));

            if (!string.IsNullOrWhiteSpace(previousSql) && !string.IsNullOrWhiteSpace(previousError))
            {
                messages.Add(new(OllamaMessageRole.Assistant, previousSql));
                messages.Add(new(OllamaMessageRole.User, $"{previousError}\n\n{_retryTemplate}"));
            }

            return messages;
        }
    }
}