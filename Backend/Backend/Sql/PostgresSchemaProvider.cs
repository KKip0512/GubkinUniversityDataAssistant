using System.Text;
using Npgsql;

namespace Backend.Sql
{
    public class PostgresSchemaProvider
    {
        private const string ColumnsSql = """
            SELECT c.relname,
                   a.attname,
                   replace(format_type(a.atttypid, a.atttypmod), 'character varying', 'varchar'),
                   t.typcategory = 'S',
                   obj_description(c.oid, 'pg_class'),
                   col_description(c.oid, a.attnum),
                   k.key
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_type t ON t.oid = a.atttypid
            LEFT JOIN LATERAL (
                SELECT CASE con.contype
                           WHEN 'p' THEN 'PRIMARY KEY'
                           ELSE format('REFERENCES %I(%I)', rc.relname, ra.attname)
                       END AS key
                FROM pg_constraint con
                LEFT JOIN pg_class rc ON rc.oid = con.confrelid
                LEFT JOIN pg_attribute ra ON ra.attrelid = con.confrelid AND ra.attnum = con.confkey[1]
                WHERE con.conrelid = c.oid
                  AND con.conkey = ARRAY[a.attnum]
                  AND (con.contype = 'p'
                       OR con.contype = 'f' AND has_column_privilege(con.confrelid, con.confkey[1], 'SELECT'))
                ORDER BY con.contype DESC
                LIMIT 1
            ) k ON true
            WHERE n.nspname = current_schema()
              AND c.relkind IN ('r', 'p', 'v', 'm') AND NOT c.relispartition
              AND a.attnum > 0 AND NOT a.attisdropped
              AND has_column_privilege(c.oid, a.attnum, 'SELECT')
            ORDER BY c.relname, a.attnum
            """;

        private record ColumnInfo(
            string Table, string? TableComment, string Name, string Type, bool IsText, string? Comment, string? Key);

        private readonly string _connString;
        private readonly int _maxValues;
        private readonly int _timeoutSeconds;
        private readonly ILogger<PostgresSchemaProvider> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private string? _schema;

        public PostgresSchemaProvider(IConfiguration config, ILogger<PostgresSchemaProvider> logger)
        {
            _connString = config.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Postgres");
            _maxValues = config.GetValue("Database:MaxSchemaValues", 20);
            _timeoutSeconds = config.GetValue("Database:TimeoutSeconds", 10);
            _logger = logger;
        }

        public async Task<string> GetSchemaAsync(CancellationToken ct = default)
        {
            if (_schema is not null)
                return _schema;

            await _lock.WaitAsync(ct);
            try
            {
                return _schema ??= await LoadAsync(ct);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<string> LoadAsync(CancellationToken ct)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            var columns = new List<ColumnInfo>();
            await using (var cmd = new NpgsqlCommand(ColumnsSql, conn) { CommandTimeout = _timeoutSeconds })
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    columns.Add(new ColumnInfo(
                        Table: reader.GetString(0),
                        TableComment: reader.IsDBNull(4) ? null : reader.GetString(4),
                        Name: reader.GetString(1),
                        Type: reader.GetString(2),
                        IsText: reader.GetBoolean(3),
                        Comment: reader.IsDBNull(5) ? null : reader.GetString(5),
                        Key: reader.IsDBNull(6) ? null : reader.GetString(6)));
                }
            }

            if (columns.Count == 0)
                throw new InvalidOperationException("В БД не найдено ни одной доступной таблицы.");

            var values = new Dictionary<ColumnInfo, IReadOnlyList<string>>();
            foreach (var column in columns.Where(c => c.IsText && c.Key is null))
            {
                var list = await LoadValuesAsync(conn, column, ct);
                if (list is not null)
                    values[column] = list;
            }

            var schema = Render(columns, values);
            _logger.LogInformation("Схема БД загружена: таблиц {Tables}, колонок {Columns}, символов {Length}",
                columns.Select(c => c.Table).Distinct().Count(), columns.Count, schema.Length);
            _logger.LogDebug("Схема БД:\n{Schema}", schema);
            return schema;
        }

        private async Task<IReadOnlyList<string>?> LoadValuesAsync(NpgsqlConnection conn, ColumnInfo column, CancellationToken ct)
        {
            var sql = $"SELECT DISTINCT {Quote(column.Name)}::text FROM {Quote(column.Table)} " +
                      $"ORDER BY 1 NULLS LAST LIMIT {_maxValues + 1}";

            await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = _timeoutSeconds };
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var result = new List<string>();
            while (await reader.ReadAsync(ct))
                result.Add(reader.IsDBNull(0) ? "NULL" : $"'{reader.GetString(0).Replace("'", "''")}'");

            return result.Count > 0 && result.Count <= _maxValues ? result : null;
        }

        private static string Render(List<ColumnInfo> columns, Dictionary<ColumnInfo, IReadOnlyList<string>> values)
        {
            var sb = new StringBuilder();

            foreach (var table in columns.GroupBy(c => c.Table))
            {
                var tableColumns = table.ToList();

                sb.Append("TABLE ").Append(table.Key).Append(" (");
                if (tableColumns[0].TableComment is { } tableComment)
                    sb.Append(" -- ").Append(tableComment);
                sb.AppendLine();

                for (var i = 0; i < tableColumns.Count; i++)
                {
                    var column = tableColumns[i];
                    sb.Append("  ").Append(column.Name).Append(' ').Append(column.Type);
                    if (column.Key is not null)
                        sb.Append(' ').Append(column.Key);
                    if (i < tableColumns.Count - 1)
                        sb.Append(',');

                    var notes = new List<string>();
                    if (column.Comment is not null)
                        notes.Add(column.Comment);
                    if (values.TryGetValue(column, out var list))
                        notes.Add($"возможные значения: {string.Join(", ", list)}");
                    if (notes.Count > 0)
                        sb.Append(" -- ").Append(string.Join("; ", notes));

                    sb.AppendLine();
                }

                sb.AppendLine(")").AppendLine();
            }

            return sb.ToString().Trim();
        }

        private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
