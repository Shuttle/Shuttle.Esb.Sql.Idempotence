namespace Shuttle.Esb.Sql.Idempotence
{
	public class IdempotenceConfiguration : IIdempotenceConfiguration
	{
	    public string ProviderName { get; set; }
	    public string ConnectionString { get; set; }
	}
}