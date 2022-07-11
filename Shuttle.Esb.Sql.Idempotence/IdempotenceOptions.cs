namespace Shuttle.Esb.Sql.Idempotence
{
    public class IdempotenceOptions
    {
        public const string SectionName = "Shuttle:Idempotence";

        public string ConnectionStringName { get; set; } = "Idempotence";
    }
}