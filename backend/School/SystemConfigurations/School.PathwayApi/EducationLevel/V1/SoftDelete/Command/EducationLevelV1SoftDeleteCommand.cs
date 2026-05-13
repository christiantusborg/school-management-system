namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Command;

public sealed record EducationLevelV1SoftDeleteCommand : IHandleableCommand<
    EducationLevelV1SoftDeleteCommand,
    EducationLevelV1SoftDeleteCommandValidator,
    EducationLevelV1SoftDeleteCommandHandler,
    EducationLevelV1SoftDeleteCommandResult>
{
    public required Guid EducationLevelId { get; init; }
}
