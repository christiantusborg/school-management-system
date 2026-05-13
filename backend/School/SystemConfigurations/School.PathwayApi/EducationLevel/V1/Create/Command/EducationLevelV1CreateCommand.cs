namespace School.PathwayApi.EducationLevel.V1.Create.Command;

public sealed record EducationLevelV1CreateCommand : IHandleableCommand<
    EducationLevelV1CreateCommand,
    EducationLevelV1CreateCommandValidator,
    EducationLevelV1CreateCommandHandler,
    EducationLevelV1CreateCommandResult>
{
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required int DisplayOrder { get; init; }
}
