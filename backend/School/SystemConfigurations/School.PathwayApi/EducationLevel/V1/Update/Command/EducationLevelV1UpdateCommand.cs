namespace School.PathwayApi.EducationLevel.V1.Update.Command;

public sealed record EducationLevelV1UpdateCommand : IHandleableCommand<
    EducationLevelV1UpdateCommand,
    EducationLevelV1UpdateCommandValidator,
    EducationLevelV1UpdateCommandHandler,
    EducationLevelV1UpdateCommandResult>
{
    public required Guid EducationLevelId { get; init; }
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required int DisplayOrder { get; init; }
}
