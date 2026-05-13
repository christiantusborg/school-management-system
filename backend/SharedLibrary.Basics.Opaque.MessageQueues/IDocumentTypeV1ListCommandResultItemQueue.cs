namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IDocumentTypeV1ListCommandResultItemQueue : IMessageQueue
{
    Guid DocumentTypeId { get; }
    string Name { get; }
}
