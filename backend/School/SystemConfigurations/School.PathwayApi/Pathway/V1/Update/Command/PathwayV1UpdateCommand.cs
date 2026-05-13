namespace School.PathwayApi.Pathway.V1.Update.Command;

public sealed record PathwayV1UpdateCommand : IHandleableCommand<
    PathwayV1UpdateCommand,
    PathwayV1UpdateCommandValidator,
    PathwayV1UpdateCommandHandler,
    PathwayV1UpdateCommandResult>
{
    public required Guid PathwayId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int MinimumYearsWorkExperience { get; init; }
    public required IReadOnlyList<Guid> DocumentTypeIds { get; init; }
    public required IReadOnlyList<Guid> AcceptedEducationLevelIds { get; init; }
}
