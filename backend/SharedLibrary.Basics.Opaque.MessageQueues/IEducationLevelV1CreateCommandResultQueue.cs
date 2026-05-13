namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IEducationLevelV1CreateCommandResultQueue : IMessageQueue
{
    Guid EducationLevelId { get; }
}
