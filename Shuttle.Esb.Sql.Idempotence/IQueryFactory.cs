using System;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Idempotence;

public interface IQueryFactory
{
    IQuery Create();
    IQuery Initialize(string inboxWorkQueueUri);
    IQuery AddDeferredMessage(Guid messageId, Guid receivedMessageId, byte[] messageBody);
    IQuery DeferredMessageSent(Guid messageId);
    IQuery GetDeferredMessages(Guid messageId);
    IQuery HasMessageBeenHandled(Guid messageId);
    IQuery ProcessingCompleted(Guid messageId);
    IQuery HasMessageBeenCompleted(Guid messageId);
    IQuery IsProcessing(Guid messageId);
    IQuery Processing(Guid messageId, string recipientInboxWorkQueueUri, int managedThreadId);
}