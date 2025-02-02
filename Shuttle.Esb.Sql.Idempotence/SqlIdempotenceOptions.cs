namespace Shuttle.Esb.Sql.Idempotence;

public class SqlIdempotenceOptions
{
    public const string SectionName = "Shuttle:Sql:Idempotence";

    public string ConnectionStringName { get; set; } = "Idempotence";
    public string Schema { get; set; } = "dbo";
}