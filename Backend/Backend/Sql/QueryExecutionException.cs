namespace Backend.Sql
{
    public class QueryExecutionException(string message, Exception innerException)
        : Exception(message, innerException);
}
