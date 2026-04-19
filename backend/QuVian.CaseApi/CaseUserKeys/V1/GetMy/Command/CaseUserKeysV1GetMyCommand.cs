namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

public sealed record CaseUserKeysV1GetMyCommand : IHandleableCommand<
    CaseUserKeysV1GetMyCommand,
    CaseUserKeysV1GetMyCommandValidator,
    CaseUserKeysV1GetMyCommandHandler,
    CaseUserKeysV1GetMyCommandResult>
{
    public required Guid CaseId { get; init; }
    public required string UserId { get; init; }
    public Guid TenantId { get; init; } = TenantConstants.DefaultTenantId;
}
