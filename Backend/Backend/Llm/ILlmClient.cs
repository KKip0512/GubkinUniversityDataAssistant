namespace Backend.Llm
{
    public interface ILlmClient
    {
        Task<string> GenerateSqlAsync(string question, string? previousSql = null, string? previousError = null, CancellationToken ct = default);
    }
}