namespace School.PathwayApi.Pathway.V1.Get.Command;

public sealed record PathwayV1GetCommand : IHandleableCommand<
    PathwayV1GetCommand,
    PathwayV1GetCommandValidator,
    PathwayV1GetCommandHandler,
    PathwayV1GetCommandResult>
{
    public required Guid PathwayId { get; init; }
}
