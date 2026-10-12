namespace Backend.Models
{
    public record QueryResponse(
    string Answer,
    string Sql,
    IReadOnlyList<string> Columns,
    IReadOnlyList<object?[]> Rows,
    string? Error)
    {
        public const string UnavailableMessage = "Сервис временно недоступен. Попробуйте позже.";
        public const string BadRequestMessage = "Некорректный запрос.";

        public static QueryResponse Success(string sql, TableResult table) =>
            new(BuildAnswer(table.Rows.Count), sql, table.Columns, table.Rows, null);

        public static QueryResponse Fail(string? sql, string error) =>
            new("", sql ?? "", [], [], error);

        private static string BuildAnswer(int rowCount) =>
            rowCount == 0 ? "По вашему запросу ничего не найдено." : $"Найдено строк: {rowCount}.";
    }
}