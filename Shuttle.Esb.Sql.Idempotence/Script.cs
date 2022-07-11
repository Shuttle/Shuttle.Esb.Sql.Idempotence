namespace Shuttle.Esb.Sql.Idempotence
{
    public class Script
    {
        public static readonly string IdempotenceServiceExists = "IdempotenceServiceExists";
        public static readonly string IdempotenceInitialize = "IdempotenceInitialize";
        public static readonly string IdempotenceProcessing = "IdempotenceProcessing";
        public static readonly string IdempotenceComplete = "IdempotenceComplete";
        public static readonly string IdempotenceIsProcessing = "IdempotenceIsProcessing";
        public static readonly string IdempotenceIsMessageHandled = "IdempotenceIsMessageHandled";
        public static readonly string IdempotenceMessageHandled = "IdempotenceMessageHandled";
        public static readonly string IdempotenceHasCompleted = "IdempotenceHasCompleted";
        public static readonly string IdempotenceSendDeferredMessage = "IdempotenceSendDeferredMessage";
        public static readonly string IdempotenceDeferredMessageSent = "IdempotenceDeferredMessageSent";
        public static readonly string IdempotenceGetDeferredMessages = "IdempotenceGetDeferredMessages";
    }
}