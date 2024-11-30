using System;
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
	    [InboxWorkQueueUri] [varchar](265) NOT NULL,
	    [DateStarted] [datetime] NOT NULL,
	    [AssignedThreadId] [int] NULL,
	    [DateThreadIdAssigned] [datetime] NULL,
	    [MessageHandled] [int],
        CONSTRAINT [PK_Idempotence] PRIMARY KEY CLUSTERED 
        (
	        [MessageId] ASC
        ) ON [PRIMARY]
    ) ON [PRIMARY]
END

IF OBJECT_ID ('{_schema}.IdempotenceHistory', 'U') IS NULL 
BEGIN
    CREATE TABLE [{_schema}].[IdempotenceHistory]
    (
	    [MessageId] [uniqueidentifier] NOT NULL,
	    [InboxWorkQueueUri] [varchar](265) NOT NULL,
	    [DateStarted] [datetime] NOT NULL,
	    [DateCompleted] [datetime] NOT NULL,
        CONSTRAINT [PK_IdempotenceHistory] PRIMARY KEY CLUSTERED 
        (
	        [MessageId] ASC
        ) ON [PRIMARY]
    ) ON [PRIMARY]
END

IF OBJECT_ID ('{_schema}.IdempotenceDeferredMessage', 'U') IS NULL 
BEGIN
    CREATE TABLE [{_schema}].[IdempotenceDeferredMessage]
    (
	    [MessageId] [uniqueidentifier] NOT NULL,
	    [MessageIdReceived] [uniqueidentifier] NOT NULL,
	    [MessageBody] [image] NOT NULL,
        CONSTRAINT [PK_IdempotenceDeferredMessage] PRIMARY KEY NONCLUSTERED 
        (
	        [MessageId] ASC
        ) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IdempotenceDeferredMessage' AND object_id = OBJECT_ID('{_schema}.IdempotenceDeferredMessage'))
BEGIN
    CREATE CLUSTERED INDEX [IX_IdempotenceDeferredMessage] ON [{_schema}].[IdempotenceDeferredMessage] 
    (
	    [MessageIdReceived] ASC
    ) ON [PRIMARY]
END

IF OBJECT_ID('{_schema}.DF_Idempotence_DateStarted', 'D') IS NULL
BEGIN
    ALTER TABLE [{_schema}].[Idempotence] ADD CONSTRAINT [DF_Idempotence_DateStarted] DEFAULT (GETUTCDATE()) FOR [DateStarted]
END

IF OBJECT_ID('{_schema}.DF_Idempotence_MessageHandled', 'D') IS NULL
BEGIN
    ALTER TABLE [{_schema}].[Idempotence] ADD CONSTRAINT [DF_Idempotence_MessageHandled] DEFAULT ((0)) FOR [MessageHandled]
END

IF OBJECT_ID('{_schema}.DF_IdempotenceHistory_DateStarted', 'D') IS NULL
BEGIN
    ALTER TABLE [{_schema}].[IdempotenceHistory] ADD CONSTRAINT [DF_IdempotenceHistory_DateStarted] DEFAULT (GETUTCDATE()) FOR [DateCompleted]
END
");
    }

    public IQuery Initialize(string inboxWorkQueueUri)
    {
        return new Query($@"
DELETE 
FROM 
	idm
FROM
	[dbo].[IdempotenceDeferredMessage] idm
INNER JOIN
	[{_schema}].[Idempotence] i on
	(
		idm.MessageId = i.MessageId
	AND
		i.InboxWorkQueueUri = @InboxWorkQueueUri
	AND
		i.MessageHandled = 0
	)

DELETE FROM [{_schema}].[Idempotence] WHERE InboxWorkQueueUri = @InboxWorkQueueUri AND MessageHandled = 0

UPDATE [dbo].[Idempotence] SET
	[AssignedThreadId] = null,
	[DateThreadIdAssigned] = null
")
            .AddParameter(Columns.InboxWorkQueueUri, Guard.AgainstNullOrEmptyString(inboxWorkQueueUri));
    }

    public IQuery AddDeferredMessage(Guid messageId, Guid receivedMessageId, byte[] messageBody)
    {
        return new Query($@"
INSERT INTO [{_schema}].[IdempotenceDeferredMessage]
(
    MessageId, 
    MessageIdReceived, 
    MessageBody
) 
VALUES 
(
    @MessageId, 
    @MessageIdReceived, 
    @MessageBody
)
")
            .AddParameter(Columns.MessageId, messageId)
            .AddParameter(Columns.MessageIdReceived, receivedMessageId)
            .AddParameter(Columns.MessageBody, messageBody);
    }

    public IQuery DeferredMessageSent(Guid messageId)
    {
        return new Query($"DELETE FROM [{_schema}].[IdempotenceDeferredMessage] WHERE MessageId = @MessageId")
            .AddParameter(Columns.MessageId, messageId);
    }

    public IQuery GetDeferredMessages(Guid messageId)
    {
        return new Query($"SELECT MessageBody FROM [{_schema}].[IdempotenceDeferredMessage] WHERE MessageIdReceived = @MessageIdReceived")
            .AddParameter(Columns.MessageIdReceived, messageId);
    }

    public IQuery HasMessageBeenHandled(Guid messageId)
    {
        return new Query($@"
IF EXISTS (SELECT NULL FROM [{_schema}].[Idempotence] WHERE MessageId = @MessageId AND MessageHandled = 1)
	SELECT 1
ELSE
	SELECT 0
")
            .AddParameter(Columns.MessageId, messageId);
    }

    public IQuery ProcessingCompleted(Guid messageId)
    {
        return new Query($@"
INSERT INTO [{_schema}].[IdempotenceHistory]
(
	MessageId,
	InboxWorkQueueUri,
	DateStarted
)
SELECT
	MessageId,
	InboxWorkQueueUri,
	DateStarted
FROM 
	{_schema}.Idempotence
WHERE
	MessageId = @MessageId

DELETE FROM [{_schema}].[Idempotence] WHERE MessageId = @MessageId
")
            .AddParameter(Columns.MessageId, messageId);
    }

    public IQuery HasMessageBeenCompleted(Guid messageId)
    {
        return new Query($@"
IF EXISTS (SELECT * FROM [{_schema}].[IdempotenceHistory] WHERE MessageId = @MessageId)
	SELECT 1
ELSE
	SELECT 0
")
            .AddParameter(Columns.MessageId, messageId);
    }

    public IQuery IsProcessing(Guid messageId)
    {
        return new Query($@"
IF EXISTS (SELECT * FROM [{_schema}].[Idempotence] WHERE MessageId = @MessageId AND AssignedThreadId IS NOT NULL)
	SELECT 1
ELSE
	SELECT 0
")
            .AddParameter(Columns.MessageId, messageId);
    }

    public IQuery Processing(Guid messageId, string recipientInboxWorkQueueUri, int managedThreadId)
    {
        return new Query($@"
IF EXISTS (SELECT * FROM [{_schema}].[Idempotence] WHERE MessageId = @MessageId AND AssignedThreadId IS NULL)
	UPDATE [{_schema}].[Idempotence] SET 
		InboxWorkQueueUri = @InboxWorkQueueUri, 
		AssignedThreadId = @AssignedThreadId, 
		DateThreadIdAssigned = GETUTCDATE()
	WHERE
		MessageId = @MessageId;
ELSE
	IF NOT EXISTS (SELECT NULL FROM [{_schema}].[IdempotenceHistory] WHERE MessageId = @MessageId)
		INSERT INTO [{_schema}].[Idempotence] (MessageId, InboxWorkQueueUri, AssignedThreadId, DateThreadIdAssigned) VALUES (@MessageId, @InboxWorkQueueUri, @AssignedThreadId, GETUTCDATE());
	ELSE
		RAISERROR ('IGNORE MESSAGE PROCESSING', 16, 0) WITH SETERROR
")
            .AddParameter(Columns.MessageId, messageId)
            .AddParameter(Columns.InboxWorkQueueUri, recipientInboxWorkQueueUri)
            .AddParameter(Columns.AssignedThreadId, managedThreadId);
    }
}