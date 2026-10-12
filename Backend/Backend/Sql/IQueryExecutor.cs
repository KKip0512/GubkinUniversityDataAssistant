using Backend.Models;

namespace Backend.Sql
{
    public interface IQueryExecutor
    {
        Task<TableResult> ExecuteAsync(string sql, CancellationToken ct = default);
    }
}
