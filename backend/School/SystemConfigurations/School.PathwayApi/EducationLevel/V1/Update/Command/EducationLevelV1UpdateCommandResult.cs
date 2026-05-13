namespace School.PathwayApi.EducationLevel.V1.Update.Command;

public sealed class EducationLevelV1UpdateCommandResult : IEducationLevelV1UpdateCommandResultQueue
{
    public required Guid EducationLevelId { get; init; }
}
