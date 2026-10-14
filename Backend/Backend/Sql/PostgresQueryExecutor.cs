using Backend.Models;
using Npgsql;

namespace Backend.Sql
{
    public class PostgresQueryExecutor : IQueryExecutor
    {
        private readonly string _connString;
        private readonly int _maxRows;
        private readonly int _timeoutSeconds;

        public PostgresQueryExecutor(IConfiguration config)
        {
            _connString = config.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Postgres");
            _maxRows = config.GetValue("Database:MaxRows", 1000);
            _timeoutSeconds = config.GetValue("Database:TimeoutSeconds", 10);
        }

        public async Task<TableResult> ExecuteAsync(string sql, CancellationToken ct = default)
        {
            try
            {
                return await ExecuteCoreAsync(sql, ct);
            }
            catch (PostgresException ex) when (IsQueryError(ex.SqlState))
            {
                throw new QueryExecutionException(ex.Message, ex);
            }
        }

        private static bool IsQueryError(string sqlState) =>
            sqlState.StartsWith("42") ||
            sqlState.StartsWith("22") ||
            sqlState.StartsWith("21") ||
            sqlState is PostgresErrorCodes.QueryCanceled or PostgresErrorCodes.ReadOnlySqlTransaction;

        private async Task<TableResult> ExecuteCoreAsync(string sql, CancellationToken ct)
        {
            var limitedSql = $"SELECT * FROM ({sql}) AS q LIMIT {_maxRows}";

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var setup = new NpgsqlCommand(
                $"SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = {_timeoutSeconds * 1000}", conn, tx))
            {
                await setup.ExecuteNonQueryAsync(ct);
            }

            await using var cmd = new NpgsqlCommand(limitedSql, conn, tx) { CommandTimeout = _timeoutSeconds + 5 };
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            var rows = new List<object?[]>();
            while (await reader.ReadAsync(ct))
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                rows.Add([.. values.Select(v => v is DBNull ? null : v)]);
            }

            return new TableResult(columns, rows);
        }
    }

}
