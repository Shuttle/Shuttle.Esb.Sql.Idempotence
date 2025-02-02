using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.Sql.Idempotence;

public class SqlIdempotenceOptionsValidator : IValidateOptions<SqlIdempotenceOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlIdempotenceOptions options)
    {
        Guard.AgainstNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
        {
            return ValidateOptionsResult.Fail(Resources.ConnectionStringOptionException);
        }

        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            return ValidateOptionsResult.Fail(Resources.SchemaOptionException);
        }

        return ValidateOptionsResult.Success;
    }
}