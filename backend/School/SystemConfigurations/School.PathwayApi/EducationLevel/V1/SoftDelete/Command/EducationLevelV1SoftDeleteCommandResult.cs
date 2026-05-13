namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Command;

public sealed class EducationLevelV1SoftDeleteCommandResult : IEducationLevelV1SoftDeleteCommandResultQueue
{
    public required Guid EducationLevelId { get; init; }
}
