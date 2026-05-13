namespace School.PathwayApi.Pathway.V1.Create.Command;

public sealed record PathwayV1CreateCommand : IHandleableCommand<
    PathwayV1CreateCommand,
    PathwayV1CreateCommandValidator,
    PathwayV1CreateCommandHandler,
    PathwayV1CreateCommandResult>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int MinimumYearsWorkExperience { get; init; }
    public required IReadOnlyList<Guid> DocumentTypeIds { get; init; }
    public required IReadOnlyList<Guid> AcceptedEducationLevelIds { get; init; }
}
