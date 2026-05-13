namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IEducationLevelV1UpdateCommandResultQueue : IMessageQueue
{
    Guid EducationLevelId { get; }
}
