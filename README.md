# Shuttle.Esb.Sql.Idempotence

Contains a sql-based `IIdempotenceService` implementation.

# Supported providers

Currently only the `System.Data.SqlClient` provider name is supported but this can easily be extended.  Feel free to give it a bash and please send a pull request if you *do* go this route.  You are welcome to create an issue and assistance will be provided where able.

~~~xml
<configuration>
  <configSections>
    <section name="idempotence" type="Shuttle.Esb.Sql.Idempotence.IdempotenceSection, Shuttle.Esb.Sql.Idempotence" />
  </configSections>

  <idempotence
    connectionStringName="connection-string-name" />
  .
  .
  .
<configuration>
~~~

