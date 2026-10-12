using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Backend.Llm
{
    public class OllamaMessagesBuilder
    {
        private readonly IReadOnlyList<OllamaMessage> _prefix;
        private readonly string _retryTemplate;

        public OllamaMessagesBuilder(PromptTemplates templates)
        {
            if (string.IsNullOrWhiteSpace(templates.System))
                throw new InvalidOperationException("Prompts: не задан system");
            if (string.IsNullOrWhiteSpace(templates.Schema))
                throw new InvalidOperationException("Prompts: не задана schema");
            if (string.IsNullOrWhiteSpace(templates.Retry))
                throw new InvalidOperationException("Prompts: не задан retry");

            var prefix = new List<OllamaMessage>
            {
                new(OllamaMessageRole.System, $"{templates.System.Trim()}\n\n{templates.Schema.Trim()}")
            };

            foreach (var example in templates.Examples)
            {
                prefix.Add(new(OllamaMessageRole.User, example.Question.Trim()));
                prefix.Add(new(OllamaMessageRole.Assistant, example.Sql.Trim()));
            }

            _prefix = prefix;
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

        public IReadOnlyList<OllamaMessage> Build(string question, string? previousSql, string? previousError)
        {
            var messages = new List<OllamaMessage>(_prefix)
            {
                new(OllamaMessageRole.User, question)
            };

            if (!string.IsNullOrWhiteSpace(previousSql) && !string.IsNullOrWhiteSpace(previousError))
            {
                messages.Add(new(OllamaMessageRole.Assistant, previousSql));
                messages.Add(new(OllamaMessageRole.User, $"{previousError}\n\n{_retryTemplate}"));
            }

            return messages;
        }
    }
}