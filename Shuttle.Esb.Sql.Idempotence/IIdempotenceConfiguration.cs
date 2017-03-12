namespace Shuttle.Esb.Sql.Idempotence
{
	public interface IIdempotenceConfiguration
	{
		string ProviderName { get; }
		string ConnectionString { get; }
	}
}