# SQL

```
PM> Install-Package Shuttle.Esb.Sql.Idempotence
```

Contains a sql-based `IIdempotenceService` implementation.

## Supported providers

Currently only the `Microsoft.Data.SqlClient` provider name is supported but this can easily be extended.  Feel free to give it a bash and please send a pull request if you *do* go this route.  You are welcome to create an issue and assistance will be provided where able.

## Configuration

```c#
services
    .AddDataAccess(builder =>
    {
        builder.AddConnectionString("Idempotence", "Microsoft.Data.SqlClient");
    })
    .AddServiceBus(builder =>
    {
        configuration.GetSection(ServiceBusOptions.SectionName)
            .Bind(builder.Options);
    })
    .AddIdempotence()
    .AddSqlIdempotence(builder =>
    {
        // defaults
        builder.Options.ConnectionStringName = "Idempotence";
        builder.Options.Schema = "dbo";

        builder.UseSqlServer();
    });
```

And the JSON configuration structure:

```json
{
  "Shuttle": {
    "ServiceBus": {
      "Sql": {
        "Idempotence": {
          "ConnectionStringName": "connection-string-name",
          "Schema": "dbo"
        }
      }
    }
  }
}
```

## Options

| Option | Default	| Description | 
| --- | --- | --- |
| `ConnectionStringName` | Idempotence | The name of the `ConnectionString` to use to connect to the idempotence store. |
| `Schema`	 | dbo | The name of the database schema to use when accessing the idempotence tables. |
