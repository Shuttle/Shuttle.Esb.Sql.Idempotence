using System;
using System.Data;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Idempotence;

public class Columns
{
    public static Column<Guid> MessageId = new("MessageId", DbType.Guid);
    public static Column<Guid> MessageReceivedId = new("MessageReceivedId", DbType.Guid);
    public static Column<string> InboxWorkQueueUri = new("InboxWorkQueueUri", DbType.AnsiString);
}