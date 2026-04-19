namespace School.ProgrammeApi.Programme.V1.List.Command;

public sealed record ProgrammeV1ListCommand : IHandleableCommand<
    ProgrammeV1ListCommand,
    ProgrammeV1ListCommandValidator,
    ProgrammeV1ListCommandHandler,
    CommandSearchResult<ProgrammeV1ListCommandResultItem>>
{
    public bool DeletedOnly { get; init; }
    public string? Ownership { get; init; }   // "core" | "partner" | null (all)
    public string? Status { get; init; }       // "Draft" | "Pending" | "Approved" | "Rejected" | null
}
