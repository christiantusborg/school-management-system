namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;

public sealed record FinalProjectStatusV1CreateCommand : IHandleableCommand<
    FinalProjectStatusV1CreateCommand,
    FinalProjectStatusV1CreateCommandValidator,
    FinalProjectStatusV1CreateCommandHandler,
    FinalProjectStatusV1CreateCommandResult>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool AllowSetByPartner { get; init; }
}
