namespace School.PathwayApi.EducationLevel.V1.Create.Command;

public sealed class EducationLevelV1CreateCommandResult : IEducationLevelV1CreateCommandResultQueue
{
    public required Guid EducationLevelId { get; init; }
}
