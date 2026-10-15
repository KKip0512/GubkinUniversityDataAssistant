using Backend.Llm;
using Backend.Models;
using Backend.Sql;

namespace Backend.Services
{
    public class QueryService(
        ILlmClient llm,
        PostgresSchemaProvider schemaProvider,
        ExampleSelector exampleSelector,
        SqlValidator validator,
        IQueryExecutor executor,
        IConfiguration config,
        ILogger<QueryService> logger)
    {
        private readonly int _maxRetries = config.GetValue("Query:MaxRetries", 2);
        private readonly int _maxQuestionLength = config.GetValue("Query:MaxQuestionLength", 500);

        public async Task<QueryResponse> AskAsync(string question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                return QueryResponse.Fail(null, "Вопрос не может быть пустым.");

            if (question.Length > _maxQuestionLength)
                return QueryResponse.Fail(null, $"Вопрос слишком длинный: максимум {_maxQuestionLength} символов.");

            var started = DateTime.UtcNow;
            logger.LogInformation("Вопрос: {Question}", question);

            string schema;
            try
            {
                schema = await schemaProvider.GetSchemaAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "Не удалось загрузить схему БД");
                return QueryResponse.Fail(null, QueryResponse.UnavailableMessage);
            }

            var examples = await exampleSelector.SelectAsync(question, ct);
            logger.LogDebug("Примеры для модели: {Examples}", string.Join(" | ", examples.Select(e => e.Question)));

            string? lastSql = null;
            string? lastError = null;

            for (var attempt = 0; attempt <= _maxRetries; attempt++)
            {
                string rawSql;
                try
                {
                    rawSql = await llm.GenerateSqlAsync(question, schema, examples, lastSql, lastError, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogError(ex, "Ошибка языковой модели (попытка {Attempt})", attempt + 1);
                    return QueryResponse.Fail(lastSql, QueryResponse.UnavailableMessage);
                }

                var validation = validator.Validate(rawSql);

                if (validation.IsNoAnswer)
                    return QueryResponse.Fail(null, validation.Error!);

                if (!validation.IsValid)
                {
                    logger.LogWarning("SQL не прошёл валидацию (попытка {Attempt}): {Error}. SQL: {Sql}",
                        attempt + 1, validation.Error, rawSql);
                    lastSql = validation.Sql ?? rawSql;
                    lastError = validation.Error;
                    continue;
                }

                try
                {
                    var table = await executor.ExecuteAsync(validation.Sql!, ct);
                    logger.LogInformation("Готово за {Ms} мс, строк: {Rows}. SQL: {Sql}",
                        (DateTime.UtcNow - started).TotalMilliseconds, table.Rows.Count, validation.Sql);
                    return QueryResponse.Success(validation.Sql!, table);
                }
                catch (QueryExecutionException ex)
                {
                    logger.LogWarning(ex, "Ошибка выполнения SQL (попытка {Attempt}): {Sql}", attempt + 1, validation.Sql);
                    lastSql = validation.Sql;
                    lastError = ex.Message;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogError(ex, "База данных недоступна. SQL: {Sql}", validation.Sql);
                    return QueryResponse.Fail(validation.Sql, QueryResponse.UnavailableMessage);
                }
            }

            return QueryResponse.Fail(lastSql, "Не удалось построить корректный запрос по этому вопросу. Попробуйте переформулировать.");
        }
    }
}