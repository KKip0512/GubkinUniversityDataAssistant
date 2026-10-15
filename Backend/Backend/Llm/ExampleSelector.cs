using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Backend.Llm
{
    public class ExampleSelector
    {
        public const string HttpClientName = "OllamaEmbeddings";

        private readonly IReadOnlyList<PromptExample> _examples;
        private readonly IHttpClientFactory _httpFactory;
        private readonly LlmOptions _options;
        private readonly ILogger<ExampleSelector> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private float[][]? _vectors;

        public ExampleSelector(
            IOptions<LlmOptions> options,
            IWebHostEnvironment env,
            IHttpClientFactory httpFactory,
            ILogger<ExampleSelector> logger)
        {
            _options = options.Value;
            _examples = LoadYaml(Path.Combine(env.ContentRootPath, _options.ExamplesPath));
            _httpFactory = httpFactory;
            _logger = logger;
        }

        private static List<PromptExample> LoadYaml(string path)
        {
            var examples = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize<List<PromptExample>?>(File.ReadAllText(path));

            if (examples is not { Count: > 0 })
                throw new InvalidOperationException($"{path}: не заданы примеры");

            foreach (var example in examples)
            {
                if (string.IsNullOrWhiteSpace(example.Question))
                    throw new InvalidOperationException($"{path}: у примера не задан question");
                if (string.IsNullOrWhiteSpace(example.Sql))
                    throw new InvalidOperationException($"{path}: у примера «{example.Question}» не задан sql");
            }

            return examples;
        }

        public async Task<IReadOnlyList<PromptExample>> SelectAsync(string question, CancellationToken ct = default)
        {
            var count = _options.ExamplesCount;
            if (_examples.Count <= count || string.IsNullOrWhiteSpace(_options.EmbeddingModel))
                return [.. _examples.Take(count)];

            try
            {
                var vectors = await GetVectorsAsync(ct);
                var query = (await EmbedAsync([question], ct))[0];

                return [.. _examples
                    .Select((example, i) => (example, score: Dot(query, vectors[i])))
                    .OrderByDescending(x => x.score)
                    .Take(count)
                    .Reverse()
                    .Select(x => x.example)];
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Не удалось подобрать похожие примеры, беру первые {Count}", count);
                return [.. _examples.Take(count)];
            }
        }

        private async Task<float[][]> GetVectorsAsync(CancellationToken ct)
        {
            if (_vectors is not null)
                return _vectors;

            await _lock.WaitAsync(ct);
            try
            {
                if (_vectors is null)
                {
                    _vectors = await EmbedAsync([.. _examples.Select(e => e.Question)], ct);
                    _logger.LogInformation("Банк примеров проиндексирован: {Count} шт.", _vectors.Length);
                }
                return _vectors;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<float[][]> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct)
        {
            var request = new OllamaEmbedRequest(
                Model: _options.EmbeddingModel!,
                Input: texts,
                KeepAlive: $"{_options.KeepAliveMinutes}m",
                Options: new OllamaEmbedOptions(NumGpu: 0));

            var http = _httpFactory.CreateClient(HttpClientName);
            using var response = await http.PostAsJsonAsync("/api/embed", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Ollama вернула {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: ct);
            if (result?.Embeddings is not { } embeddings || embeddings.Count != texts.Count)
                throw new InvalidOperationException("Ollama вернула неожиданный ответ на запрос эмбеддингов.");

            return [.. embeddings.Select(Normalize)];
        }

        private static float[] Normalize(float[] vector)
        {
            var length = MathF.Sqrt(vector.Sum(x => x * x));
            return length == 0 ? vector : [.. vector.Select(x => x / length)];
        }

        private static float Dot(float[] a, float[] b)
        {
            var sum = 0f;
            for (var i = 0; i < a.Length; i++)
                sum += a[i] * b[i];
            return sum;
        }
    }
}
