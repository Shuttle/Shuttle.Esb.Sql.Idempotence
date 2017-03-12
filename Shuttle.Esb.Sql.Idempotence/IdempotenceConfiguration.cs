namespace Shuttle.Esb.Sql.Idempotence
{
	public class IdempotenceConfiguration : IIdempotenceConfiguration
	{
	    public string IdempotenceServiceProviderName { get; set; }
	    public string IdempotenceServiceConnectionString { get; set; }
	}
}