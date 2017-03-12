namespace Shuttle.Esb.Sql.Idempotence
{
	public interface IIdempotenceConfiguration
	{
		string IdempotenceServiceProviderName { get; }
		string IdempotenceServiceConnectionString { get; }
	}
}