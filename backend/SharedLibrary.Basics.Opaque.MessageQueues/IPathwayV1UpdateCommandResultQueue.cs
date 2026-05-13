namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IPathwayV1UpdateCommandResultQueue : IMessageQueue
{
    Guid PathwayId { get; }
}
