namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;

public sealed record FinalProjectStatusV1UpdateCommand : IHandleableCommand<
    FinalProjectStatusV1UpdateCommand,
    FinalProjectStatusV1UpdateCommandValidator,
    FinalProjectStatusV1UpdateCommandHandler,
    FinalProjectStatusV1UpdateCommandResult>
{
    public required int FinalProjectStatusId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool AllowSetByPartner { get; init; }
}
