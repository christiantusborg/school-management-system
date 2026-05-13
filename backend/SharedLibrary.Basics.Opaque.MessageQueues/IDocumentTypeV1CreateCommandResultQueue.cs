namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IDocumentTypeV1CreateCommandResultQueue : IMessageQueue
{
    Guid DocumentTypeId { get; }
}
