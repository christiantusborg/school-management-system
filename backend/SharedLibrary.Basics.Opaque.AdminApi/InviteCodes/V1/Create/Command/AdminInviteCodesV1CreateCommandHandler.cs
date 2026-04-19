namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminInviteCodesV1CreateCommandHandler(IInviteCodeRepository repository)
    : ICommandHandler<AdminInviteCodesV1CreateCommand, AdminInviteCodesV1CreateCommandResult,
        InviteCode, IInviteCodeRepository>
{
    public async Task<SuccessOrFailure<AdminInviteCodesV1CreateCommandResult>> HandleAsync(
        AdminInviteCodesV1CreateCommand command, CancellationToken cancellationToken)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        var invite = new InviteCode
        {
            Code = code,
            CreatedByUserId = command.CreatedByUserId,
            AssignedRole = command.AssignedRole,
            ExpiresAt = DateTime.UtcNow.AddDays(command.ExpirationDays)
        };

        repository.Add(invite);


        return new SuccessOrFailure<AdminInviteCodesV1CreateCommandResult>(new AdminInviteCodesV1CreateCommandResult
        {
            Code = code,
            AssignedRole = invite.AssignedRole,
            ExpiresAt = invite.ExpiresAt
        });
    }
}
