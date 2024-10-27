using System;
using System.Data;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Idempotence;

public class Columns
{
    public static Column<Guid> MessageId = new("MessageId", DbType.Guid);
    public static Column<Guid> MessageIdReceived = new("MessageIdReceived", DbType.Guid);
    public static Column<string> InboxWorkQueueUri = new("InboxWorkQueueUri", DbType.AnsiString);
    public static Column<byte[]> MessageBody = new("MessageBody", DbType.Binary);
    public static Column<int> AssignedThreadId = new("AssignedThreadId", DbType.Int32);
}