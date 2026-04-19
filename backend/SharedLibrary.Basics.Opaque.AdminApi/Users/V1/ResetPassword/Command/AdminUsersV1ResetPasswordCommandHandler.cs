namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOpaqueCredentialRepository opaqueCredentialRepository,
    IOpaqueRecoveryCodeRepository recoveryCodeRepository,
    SessionTokenService sessionTokenService,
    ITransientStateCache transientStateCache)
    : ICommandHandler<AdminUsersV1ResetPasswordCommand, AdminUsersV1ResetPasswordCommandResult,
        OpaqueCredential, IOpaqueCredentialRepository>
{
    public async Task<SuccessOrFailure<AdminUsersV1ResetPasswordCommandResult>> HandleAsync(
        AdminUsersV1ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<AdminUsersV1ResetPasswordCommandResult>.EntityNotFound(
                typeof(AdminUsersV1ResetPasswordCommand));

        var credentialSpec = new Specification<OpaqueCredential>()
            .AddWhere(x => x.UserId == command.UserId);
        var credential = await opaqueCredentialRepository.GetAsync(credentialSpec, cancellationToken).ConfigureAwait(false);
        if (credential is not null)
            opaqueCredentialRepository.Remove(credential);

        var recoverySpec = new Specification<OpaqueRecoveryCode>()
            .AddWhere(x => x.UserId == command.UserId);
        var recoveryCodes = await recoveryCodeRepository.SearchAsync(recoverySpec, cancellationToken).ConfigureAwait(false);
        foreach (var code in recoveryCodes)
            recoveryCodeRepository.Remove(code);

        await sessionTokenService.RevokeAllUserTokensAsync(command.UserId, cancellationToken);

        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        await transientStateCache.SetAsync($"reset:{resetToken}", command.UserId, TimeSpan.FromHours(1));

        return new SuccessOrFailure<AdminUsersV1ResetPasswordCommandResult>(
            new AdminUsersV1ResetPasswordCommandResult { ResetToken = resetToken });
    }
}
