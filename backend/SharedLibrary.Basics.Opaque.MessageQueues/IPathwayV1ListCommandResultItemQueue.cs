namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IPathwayV1ListCommandResultItemQueue : IMessageQueue
{
    Guid PathwayId { get; }
}
