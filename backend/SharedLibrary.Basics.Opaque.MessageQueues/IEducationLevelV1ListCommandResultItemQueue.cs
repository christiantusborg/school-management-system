namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IEducationLevelV1ListCommandResultItemQueue : IMessageQueue
{
    Guid EducationLevelId { get; }
}
