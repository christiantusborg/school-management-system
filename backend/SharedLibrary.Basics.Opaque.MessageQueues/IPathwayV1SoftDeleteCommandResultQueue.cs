namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IPathwayV1SoftDeleteCommandResultQueue : IMessageQueue
{
    Guid PathwayId { get; }
}
