namespace SharedLibrary.Basics.Opaque.MessageQueues;

public interface IEducationLevelV1SoftDeleteCommandResultQueue : IMessageQueue
{
    Guid EducationLevelId { get; }
}
