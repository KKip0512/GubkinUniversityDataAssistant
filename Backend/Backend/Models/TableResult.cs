namespace Backend.Models
{
    public record TableResult(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows);
}