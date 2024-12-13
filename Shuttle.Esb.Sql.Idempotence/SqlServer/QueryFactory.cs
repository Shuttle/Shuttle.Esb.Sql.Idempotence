using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Idempotence.SqlServer;

public class QueryFactory : IQueryFactory
{
    private readonly string _schema;

    public QueryFactory(IOptions<SqlIdempotenceOptions> sqlIdempotenceOptions)
    {
        _schema = Guard.AgainstNull(Guard.AgainstNull(sqlIdempotenceOptions).Value).Schema;
    }

    public IQuery Create()
    {
        return new Query($@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_schema}')
BEGIN
    EXEC('CREATE SCHEMA {_schema}');
END

IF OBJECT_ID ('{_schema}.Idempotence', 'U') IS NULL 
BEGIN
    CREATE TABLE [{_schema}].[Idempotence]
    (
	    [MessageId] [uniqueidentifier] NOT NULL,
	    [MessageReceivedId] [uniqueidentifier] NOT NULL,
	    [InboxWorkQueueUri] [varchar](265) NOT NULL,
	    [DateRegistered] [datetime] NOT NULL,
	    [DateCompleted] [datetime] NULL,
        CONSTRAINT [PK_Idempotence] PRIMARY KEY CLUSTERED 
        (
	        [MessageId] ASC
        ) ON [PRIMARY]
    ) ON [PRIMARY]
END
");
    }

    public IQuery Register(TransportMessage transportMessage)
    {
        Guard.AgainstNull(transportMessage);

        return new Query($@"
INSERT INTO [{_schema}].[Idempotence]
(
    MessageId,
    MessageReceivedId,
    InboxWorkQueueUri,
    DateRegistered
)
VALUES
(
    @MessageId,
    @MessageReceivedId,
    @InboxWorkQueueUri,
    GETUTCDATE()
);
")
            .AddParameter(Columns.MessageId, transportMessage.MessageId)
            .AddParameter(Columns.MessageReceivedId, transportMessage.MessageReceivedId)
            .AddParameter(Columns.InboxWorkQueueUri, transportMessage.RecipientInboxWorkQueueUri);
    }

    public IQuery Handled(TransportMessage transportMessage)
    {
        return new Query($@"
UPDATE 
    [{_schema}].[Idempotence] 
SET
    DateCompleted = GETUTCDATE()
WHERE
    MessageId = @MessageId
")
            .AddParameter(Columns.MessageId, transportMessage.MessageId);
    }

    public IQuery Contains(TransportMessage transportMessage)
    {
        return new Query($@"
IF EXISTS
(
    SELECT 
        NULL 
    FROM 
        [{_schema}].[Idempotence] 
    WHERE 
        MessageId = @MessageId
)
    SELECT 1
ELSE
    SELECT 0
")
            .AddParameter(Columns.MessageId, transportMessage.MessageId);
    }
}