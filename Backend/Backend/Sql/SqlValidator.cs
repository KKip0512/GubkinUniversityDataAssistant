using System.Text.RegularExpressions;

namespace Backend.Sql
{
    public record ValidationResult(bool IsValid, string? Sql, string? Error, bool IsNoAnswer = false);

    public partial class SqlValidator
    {
        [GeneratedRegex(@"\b(insert|update|delete|drop|alter|truncate|create|grant|revoke|copy|call|do|execute|exec|" +
            @"vacuum|set|set_config|reset|listen|notify|pg_sleep\w*|pg_read_\w+|pg_ls_\w+|pg_terminate_backend|" +
            @"pg_cancel_backend|lo_\w+|dblink\w*)\b|\bu&['""]",
            RegexOptions.IgnoreCase)]
        private static partial Regex ForbiddenKeywords { get; }

        [GeneratedRegex(@"^\s*(select|with)\b", RegexOptions.IgnoreCase)]
        private static partial Regex StartsWithSelect { get; }

        [GeneratedRegex(@"```(?:sql)?\s*(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
        private static partial Regex Markdown { get; }

        [GeneratedRegex(@"/\*.*?\*/|--[^\r\n]*", RegexOptions.Singleline)]
        private static partial Regex Comments { get; }

        public ValidationResult Validate(string? unchangedSql)
        {
            if (string.IsNullOrWhiteSpace(unchangedSql))
                return new(false, null, "Пустой SQL");

            var sql = Clean(unchangedSql);

            if (sql.StartsWith("-- NO_ANSWER", StringComparison.OrdinalIgnoreCase))
                return new(false, null, "Не удалось найти в базе данных информацию по этому вопросу.", IsNoAnswer: true);

            sql = StripComments(sql);

            if (!StartsWithSelect.IsMatch(sql))
                return new(false, sql, "Разрешены только запросы SELECT.");

            if (sql.Contains(';'))
                return new(false, sql, "Разрешён только один SQL-запрос, без ';' в середине.");

            var match = ForbiddenKeywords.Match(sql);
            if (match.Success)
                return new(false, sql, $"Запрещённое ключевое слово: {match.Value.ToUpperInvariant()}.");

            return new(true, sql, null);
        }

        private static string Clean(string sql)
        {
            sql = sql.Trim();

            var fence = Markdown.Match(sql);
            if (fence.Success) sql = fence.Groups[1].Value.Trim();

            return sql;
        }

        private static string StripComments(string sql) =>
            Comments.Replace(sql, " ").Trim().TrimEnd(';', ' ', '\n', '\r', '\t');
    }
}