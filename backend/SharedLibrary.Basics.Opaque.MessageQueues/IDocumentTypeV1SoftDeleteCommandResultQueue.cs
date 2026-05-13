namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IDocumentTypeV1SoftDeleteCommandResultQueue : IMessageQueue
{
    Guid DocumentTypeId { get; }
}
