using System;
using System.Data;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Idempotence
{
    public class Columns
    {
        public static Column<Guid> MessageId = new Column<Guid>("MessageId", DbType.Guid);
        public static Column<Guid> MessageIdReceived = new Column<Guid>("MessageIdReceived", DbType.Guid);
        public static Column<string> InboxWorkQueueUri = new Column<string>("InboxWorkQueueUri", DbType.AnsiString);
        public static Column<byte[]> MessageBody = new Column<byte[]>("MessageBody", DbType.Binary);
        public static Column<int> AssignedThreadId = new Column<int>("AssignedThreadId", DbType.Int32);
    }
}