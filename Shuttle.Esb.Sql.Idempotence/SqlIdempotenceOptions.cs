namespace Shuttle.Esb.Sql.Idempotence;

public class SqlIdempotenceOptions
{
    public const string SectionName = "Shuttle:SqlIdempotence";

    public string ConnectionStringName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
}